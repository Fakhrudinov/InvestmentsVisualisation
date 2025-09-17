using DataAbstraction.Interfaces;
using DataAbstraction.Models.BaseModels;
using DataAbstraction.Models.MoneyByMonth;
using DataAbstraction.Models.SecCodes;
using DataAbstraction.Models.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Xml;
using UserInputService;

namespace HttpDataRepository
{
    public class WebData : IWebData
    {
        private readonly ILogger<WebData> _logger;
        private readonly IOptionsSnapshot<WebDiviPageSettings> _namedOptions;
        private WebDiviPageSettings ? _options;
        private InputHelper _helper;
		private IMySqlSecCodesRepository _repository;

		public WebData(
            ILogger<WebData> logger, 
            IOptionsSnapshot<WebDiviPageSettings> namedOptions,
			IMySqlSecCodesRepository repository,
			InputHelper helper)
        {
            _logger = logger;
            _namedOptions = namedOptions;
            _helper = helper;
            _repository = repository;
        }

        public async Task<DohodDivsAndDatesModel?> GetDividentsTableFromDohod(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WebData " +
                $"GetDividentsTableFromDohod called");

            _options=_namedOptions.Get("Dohod");

			//download html and get table with dividents
			string[]? rawDataTableSplitted = await DownloadAndGetHtmlTableWithDividents(cancellationToken);
            if (rawDataTableSplitted is null)
            {
                return null;
            }
            DohodDivsAndDatesModel result = new DohodDivsAndDatesModel();
            result.DohodDivs = GetDividentsFromHtml(rawDataTableSplitted);
            result.DohodDates = GetDividentsDatesFromHtml(rawDataTableSplitted);

            return result;
        }

        public async Task<List<SecCodeAndDividentModel>?> GetDividentsTableFromVsdelke(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WebData " +
                $"GetDividentsTableFromVsdelke called");

            _options=_namedOptions.Get("Vsdelke");
            return await GetDividents(cancellationToken);
        }
        public async Task<List<SecCodeAndDividentModel>?> GetDividentsTableFromSmartLab(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WebData " +
                $"GetDividentsTableFromSmartLab called");

            _options=_namedOptions.Get("SmLab");
            return await GetDividents(cancellationToken);
        }

        private async Task<List<SecCodeAndDividentModel> ?> GetDividents(CancellationToken cancellationToken)
        {
            string[] ? rawDataTableSplitted = await DownloadAndGetHtmlTableWithDividents(cancellationToken);
            if (rawDataTableSplitted is null)
            {
                return null;
            }

            //заполним дивиденты
            List<SecCodeAndDividentModel> result = GetDividentsFromHtml(rawDataTableSplitted);


            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WebData GetDividents " +
                $"result list count is {result.Count}");

            return result;
        }


        private List<SecCodeAndTimeToCutOffModel>? GetDividentsDatesFromHtml(string[] rawDataTableSplitted)
        {
            List<SecCodeAndTimeToCutOffModel> result = new List<SecCodeAndTimeToCutOffModel>();            

            for (int i = _options.NumberOfRowToStartSearchData; i < rawDataTableSplitted.Length; i++)
            {
                string[] dataTr = rawDataTableSplitted[i].Split(_options.TableCellSplitter);

                //get divident tiker
                SecCodeAndTimeToCutOffModel newDiv = new SecCodeAndTimeToCutOffModel();
                newDiv.SecCode = GetSecCodeFromTD(dataTr[_options.NumberOfCellWithHref].Replace("\"", "'"));

                if (newDiv.SecCode is null)
                {
                    // href with tiker name not found
                    continue;
                }

                //get divident dateTime
                newDiv.Date = GetDateFromTD(dataTr[_options.NumberOfCellWithDiscont + 2]);
                if (newDiv.Date == DateTime.MinValue)
                {
                    // date not found
                    continue;
                }

                // get DSI Index - TD 12
                newDiv.DSIIndex = CleanTextFromHtmlTags(dataTr[_options.NumberOfCellWithDiscont + 5]);

                // get divident value
                string div = CleanTextFromHtmlTags(dataTr[_options.NumberOfCellWithDiscont].Replace("\"", "'"));
                div = div.Replace(".", ",");
                if (div.Equals("0%") ||
                    div.Equals("0,0%") ||
                    div.Equals("0,00%"))
                {
                    // do not add empty values
                    continue;
                }


                /// пробуем найти, если нет - добавляем.
                /// если есть, сравниваем, одинаковые ПРОПУСКАЕМ
                /// если больше - заменяем существуещее значение меньшим
                int index = result.FindIndex(res => res.SecCode == newDiv.SecCode);
                if (index >= 0)
                {
                    if (result[index].Date.Equals(newDiv.Date))
                    {
                        // ПРОПУСКАЕМ одинаковые
                        continue;
                    }
                    else if(result[index].Date > newDiv.Date)
                    {
                        // save/replase to earler date
                        result[index].Date = newDiv.Date;
                    }
                }
                else
                {
                    // не нашли, добавляем
                    result.Add(newDiv);
                }
            }            

            return result;         
        }

        private DateTime GetDateFromTD(string rawText)
        {
            rawText = CleanTextFromHtmlTags(rawText);

            DateTime dateValue = DateTime.MinValue;
            if (DateTime.TryParse(rawText, out dateValue))
            {
                //
            }
            
            return dateValue;
        }

        private List<SecCodeAndDividentModel> GetDividentsFromHtml(string[] rawDataTableSplitted)
        {
            List<SecCodeAndDividentModel> result = new List<SecCodeAndDividentModel>();
            for (int i = _options.NumberOfRowToStartSearchData; i < rawDataTableSplitted.Length; i++)
            {
                string[] dataTr = rawDataTableSplitted[i].Split(_options.TableCellSplitter);
                SecCodeAndDividentModel newDiv = new SecCodeAndDividentModel();

                //get divident tiker
                // for vsdelke.ru - alternative get SecCode method - from <font class="tiker">BELU</font>
                if (_options.BaseUrl.Contains("vsdelke.ru"))
                {
                    newDiv.SecCode = GetSecCodeFromVsdelkeTD(dataTr[_options.NumberOfCellWithHref].Replace("\"", "'"));
                }
                else // other contain seccode in href
                {
                    newDiv.SecCode = GetSecCodeFromTD(dataTr[_options.NumberOfCellWithHref].Replace("\"", "'"));
                }

                // in smart lab both AO and AP strings looks to AO page, so we check this markets and correct tikers name
                if (_options.BaseUrl.Contains("smart-lab.ru"))
                {
                    newDiv.SecCode = CleanTextFromHtmlTags(dataTr[_options.NumberOfCellWithHref].Replace("\"", "'"));
                }

                ////debug point
                //if (newDiv.SecCode.Contains("WTCM"))
                //{
                //    Console.WriteLine();
                //}

                if (newDiv.SecCode is null)
                {
                    // href with tiker name not found
                    continue;
                }

                // get divident value
                newDiv.Divident = CleanTextFromHtmlTags(dataTr[_options.NumberOfCellWithDiscont].Replace("\"", "'"));

                newDiv.Divident = newDiv.Divident.Replace(".", ",");

                if (newDiv.Divident.Equals("0%") ||
                    newDiv.Divident.Equals("0,0%") ||
                    newDiv.Divident.Equals("0,00%"))
                {
                    // do not add empty values
                    continue;
                }

                _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WebData GetDividents " +
                    $"Add SecCodeAndDividentModel='{newDiv.SecCode}' '{newDiv.Divident}'");

                /// пробуем найти, если нет - добавляем.
                /// если есть, сравниваем, одинаковые ПРОПУСКАЕМ
                /// если меньше, чем есть - ПРОПУСКАЕМ
                /// если больше - заменяем существуещее значение
                int index = result.FindIndex(res => res.SecCode == newDiv.SecCode);
                if (index >= 0)
                {
                    if (result[index].Divident.Equals(newDiv.Divident))
                    {
                        // ПРОПУСКАЕМ одинаковые
                        continue;
                    }
                    else
                    {
                        bool biggerThenExist = CompareTwoDividentsValues(result[index].Divident, newDiv.Divident);
                        if (biggerThenExist)
                        {
                            //если больше - заменяем существуещее значение
                            result[index].Divident = newDiv.Divident;
                        }
                    }
                }
                else
                {
                    // не нашли, добавляем
                    result.Add(newDiv);
                }
            }

            return result;
        }

        private async Task<string[]?> DownloadAndGetHtmlTableWithDividents(CancellationToken cancellationToken)
        {
            //get fool html source
            string? rawHtmlSource = await GetFullHtmlPage(cancellationToken, _options.BaseUrl);

            if (rawHtmlSource is null || 
                !rawHtmlSource.Contains(_options.StartWord) || 
                !rawHtmlSource.Contains(_options.EndWord))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WebData " +
                    $"DownloadAndGetHtmlTableWithDividents rawHtmlSource is null " +
                    $"or StartWord pointer '{_options.StartWord}' is not found " +
                    $"or EndWord pointer '{_options.EndWord}' is not found!");

                return null;
            }

            //cut table from full html source
            int startIndex = rawHtmlSource.IndexOf(_options.StartWord);
            int endIndex = rawHtmlSource.Substring(startIndex).IndexOf(_options.EndWord);

            string rawDataTable = rawHtmlSource.Substring(startIndex, endIndex);
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WebData " +
                $"DownloadAndGetHtmlTableWithDividents splitted text lenght is {rawDataTable.Length}");

            //split
            string[] rawDataTableSplitted = rawDataTable.Split(_options.TableRowSplitter);

            return rawDataTableSplitted;
        }

        private bool CompareTwoDividentsValues(string divident1, string divident2)
        {
            decimal divExist = GetDecimalFromDivString(divident1);
            decimal divNew = GetDecimalFromDivString(divident2);

            if (divExist < divNew)
            {
                return true;
            }

            return false;
        }

        private decimal GetDecimalFromDivString(string divident)
        {
            divident = divident.Replace("%", "");
            divident = divident.Replace(".", ",");
            decimal dec;
            if (decimal.TryParse(divident, out dec))
            {
                return dec;
            }
            else
            {
                divident = divident.Replace(",", ".");
                if (decimal.TryParse(divident, out dec))
                {
                    return dec;
                }
            }

            return dec;
        }


        private string GetSecCodeFromVsdelkeTD(string rawText)
        {
            //<td aria-label="Компания">
            //  <a class="a_emitent" title="Посмотреть полный календарь дивидендов компании Белуга Групп ПАО ао" href="/dividendy/beluga.html">Белуга ао</a>
            //  <font class="tiker">BELU</font>
            //</td>

            int startIndex = rawText.IndexOf("tiker");
            if (startIndex == -1)
            {
                return null;
            }

            string result = rawText.Substring(startIndex + 7); // 6 = lenght of href=\"
            int endIndex = result.IndexOf("</font>");
            result = result.Substring(0, endIndex);

            return result;
        }
        private string ? GetSecCodeFromTD(string rawText)
        {
            //<a href="/ik/analytics/dividend/trmk">
            //<a href="/forum/GAZP"> //"<a href=\"/forum/GAZP\">Газпром</a></td>\n\t"

            int startIndex = rawText.IndexOf("href='");
            if (startIndex == -1)
            {
                return null;
            }

            string result = rawText.Substring(startIndex + 6); // 6 = lenght of href=\"
            int endIndex = result.IndexOf("'"); // search "
            result = result.Substring(0, endIndex);

            //char aaa = result[result.Length - 1];
            
            if (result[result.Length - 1].Equals('/'))
            {
                result = result.Substring(0, result.Length - 1);
            }
            
            int lastIndexOfSlash = result.LastIndexOf('/');
            result = result.Substring(lastIndexOfSlash + 1);

            return result.ToUpper();
        }

        private string CleanTextFromHtmlTags(string rawText)
        {
            //firstly remove all before '>'
            rawText = rawText.Substring(rawText.IndexOf(">") + 1);

            // for vsdelke - remove all in ()
            if (rawText.Contains('('))
            {
                rawText = rawText.Substring(0, rawText.IndexOf("("));
            }            

            foreach (string removeThis in _options.CleanWordsFromCell)
            {
                rawText = rawText.Replace(removeThis, "");
            }

            rawText = rawText.Replace(" ", "");

            return rawText;
        }

        private async Task <string?> GetFullHtmlPage(CancellationToken cancellationToken, string baseUrl)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WebData " +
                $"GetFullHtmlPage called, baseURL={baseUrl}");

            string rawHtmlSource = string.Empty;

            using (var client = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            }))
            {
                try
                {
                    HttpResponseMessage response = client.GetAsync(baseUrl, cancellationToken).Result;
                    response.EnsureSuccessStatusCode();
                    rawHtmlSource = response.Content.ReadAsStringAsync(cancellationToken).Result;

                    _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WebData " +
                        $"GetFullHtmlPage rawHtmlSource lenght is {rawHtmlSource.Length}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WebData " +
                        $"GetFullHtmlPage Exception at baseURL={baseUrl}, Message is: {ex.Message}");

                    return null;
                }
            }

            return rawHtmlSource;
        }

        public async Task<List<ExpectedDividentsFromWebModel>?> GetFutureDividentsTableFromDohod(
            CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WebData " +
                $"GetDividentsTableFromDohod called");

            _options=_namedOptions.Get("Dohod");

			//download html and get table with dividents
			string[]? rawDataTableSplitted = await DownloadAndGetHtmlTableWithDividents(cancellationToken);
            if (rawDataTableSplitted is null)
            {
                return null;
            }

            List<ExpectedDividentsFromWebModel> result = new List<ExpectedDividentsFromWebModel>();

			for (int i = _options.NumberOfRowToStartSearchData; i < rawDataTableSplitted.Length; i++)
			{
				string[] dataTr = rawDataTableSplitted[i].Split(_options.TableCellSplitter);
				ExpectedDividentsFromWebModel newDiv = new ExpectedDividentsFromWebModel();

                //get divident dateTime
                newDiv.Date = GetDateFromTD(dataTr[_options.NumberOfCellWithDiscont + 2]);
                if (newDiv.Date == DateTime.MinValue)
                {
                    // date not found
                    continue;
                }


                // get divident value for one pieces of shares
                string dividentPerPieceString = CleanTextFromHtmlTags(dataTr[_options.NumberOfCellWithDiscont - 3]);
                if (_helper.IsDecimal(dividentPerPieceString))
                {
                    newDiv.DividentToOnePiece = _helper.GetDecimalFromString(dividentPerPieceString);
                }
                if (newDiv.DividentToOnePiece == 0)
                {
                    // do not add empty values
                    continue;
                }


                newDiv.SecCode = GetSecCodeFromTD(dataTr[_options.NumberOfCellWithHref].Replace("\"", "'"));
				if (newDiv.SecCode is null)
				{
					// href with tiker name not found
					continue;
				}


                //get bool - is approoved?
                if (dataTr[_options.NumberOfCellWithDiscont - 2].Contains("<img"))
                {
                    newDiv.IsConfirmed = true;
                }

				// get divident percents value
				string dividentPercentString = CleanTextFromHtmlTags(dataTr[_options.NumberOfCellWithDiscont].Replace("%", ""));
				if (_helper.IsDecimal(dividentPercentString))
				{
					newDiv.DividentInPercents = _helper.GetDecimalFromString(dividentPercentString);
				}

				result.Add(newDiv);
			}

            if (result.Count == 0)
            {
                return null;
            }
			return result;
		}

		public async Task<bool> CreateNewSecCode(CancellationToken cancellationToken, string isin)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WebData " +
                $"CreateNewSecCode called");

			/// GET xml with seccode by search request like https://iss.moex.com/iss/securities.xml?q={isin}&is_trading=1
			/// if ok GET secCode from xml
			/// if ok GET xml with instrument parametres by request like https://iss.moex.com/iss/securities/{secCode}?primary_board=1
			/// if ok GET instrument from xml
			/// if ok PUT new instrument to DataBase
			/// renew secodes list
			/// 
			/// 

			// GET xml with seccode by search request like https://iss.moex.com/iss/securities.xml?q={isin}&is_trading=1
			string? xmlWithSecCode = await GetFullHtmlPage(
                cancellationToken,
				$"https://iss.moex.com/iss/securities.xml?q={isin}&is_trading=1");
            if (xmlWithSecCode is null || xmlWithSecCode.Equals(String.Empty) ||xmlWithSecCode.Equals(""))
            {
				_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WebData CreateNewSecCode action GetFullHtmlPage" +
                    $" for 'https://iss.moex.com/iss/securities.xml?q={isin}&is_trading=1' return null result.");
				return false;
            }


			// if ok GET secCode from xml
			string? secCode = GetSecCodeByXmlText(xmlWithSecCode);
			if (secCode is null)
			{
				return false;
			}


			// if ok GET xml with instrument parametres by request like https://iss.moex.com/iss/securities/{secCode}?primary_board=1
			string? xmlWithInstrumentParams = await GetFullHtmlPage(
                cancellationToken,
                $"https://iss.moex.com/iss/securities/{secCode}?primary_board=1");
			if (xmlWithInstrumentParams is null || xmlWithInstrumentParams.Equals(String.Empty) ||xmlWithInstrumentParams.Equals(""))
			{
				_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WebData CreateNewSecCode action GetFullHtmlPage" +
					$" for 'https://iss.moex.com/iss/securities/{secCode}?primary_board=1' return null result.");
				return false;
			}


			// if ok GET instrument from xml
			SecCodeInfo? secCodeModel = GetSecCodeInfoModelByXmlText(xmlWithInstrumentParams);
            if (secCodeModel is null)
            {
                return false;
            }

			// if ok PUT new instrument to DataBase
			// renew secodes list
			string result = await _repository.CreateNewSecCode(cancellationToken, secCodeModel);
			if (!result.Equals("1"))
			{
				_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WebData CreateNewSecCode action " +
                    $"Create_repository.CreateNewSecCodeNewSecCode error: Добавление не удалось. \r\n{result}");
				return false;
			}

			_repository.RenewStaticSecCodesList(cancellationToken);

			return true;
		}

		private SecCodeInfo? GetSecCodeInfoModelByXmlText(string rawHtmlSource)
		{
			XmlDocument moexXml = new XmlDocument();
			moexXml.LoadXml(rawHtmlSource);

			XmlElement? xRoot = moexXml.DocumentElement;
			if (xRoot is not null)
			{
				SecCodeInfo secCodeInfo = new SecCodeInfo();
				foreach (XmlElement xnode in xRoot)
				{
					foreach (XmlNode childNode in xnode.ChildNodes)
					{
						if (childNode.Name == "rows")
						{
							foreach (XmlNode rowsChildNode in childNode.ChildNodes)
							{
								if (rowsChildNode.Name == "row" && rowsChildNode.Attributes is not null)
								{
									XmlAttribute? attributeName = rowsChildNode.Attributes["name"];


									if (attributeName is not null)
									{
										if (attributeName.Value.Equals("SECID"))
										{
											XmlAttribute? attributeValue = rowsChildNode.Attributes["value"];
											if (attributeValue is not null)
											{
												secCodeInfo.SecCode = attributeValue.Value;
											}
										}


										if (attributeName.Value.Equals("NAME"))
										{
											XmlAttribute? attributeValue = rowsChildNode.Attributes["value"];
											if (attributeValue is not null)
											{
												secCodeInfo.FullName = attributeValue.Value;
											}
										}


										if (attributeName.Value.Equals("SHORTNAME"))
										{
											XmlAttribute? attributeValue = rowsChildNode.Attributes["value"];
											if (attributeValue is not null)
											{
												secCodeInfo.Name = attributeValue.Value;
											}
										}


										if (attributeName.Value.Equals("ISIN"))
										{
											XmlAttribute? attributeValue = rowsChildNode.Attributes["value"];
											if (attributeValue is not null)
											{
												secCodeInfo.ISIN = attributeValue.Value;
											}
										}


										if (attributeName.Value.Equals("COUPONFREQUENCY"))
										{
											XmlAttribute? attributeValue = rowsChildNode.Attributes["value"];

											if (attributeValue is not null && !attributeValue.Value.Equals("12"))
											{
												int paymentRepYear;
												if (Int32.TryParse(attributeValue.Value, out paymentRepYear))
												{
													secCodeInfo.PaysPerYear = paymentRepYear;
												}
											}
										}


										//FACEVALUE остаток объема после частичного погашения


										if (attributeName.Value.Equals("GROUP"))
										{
											XmlAttribute? attributeValue = rowsChildNode.Attributes["value"];
											if (attributeValue is not null)
											{
												if (attributeValue.Value.Equals("stock_bonds"))
												{
													secCodeInfo.SecBoard = 2;
												}
												else if (attributeValue.Value.Equals("stock_shares"))
												{
													secCodeInfo.SecBoard = 1;
												}
												else
												{
													_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WebData " +
                                                        $"GetSecCodeInfoModelByXmlText error: Type not recognized!");
													return null;
												}

											}
										}


										if (attributeName.Value.Equals("MATDATE"))
										{
											XmlAttribute? attributeValue = rowsChildNode.Attributes["value"];
											if (attributeValue is not null)
											{
												DateTime dateValue;
												if (DateTime.TryParse(attributeValue.Value, out dateValue))
												{
													if (dateValue < DateTime.Now)
													{
														secCodeInfo.ExpiredDate = dateValue;
													}
												}
												else
												{
													_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WebData " +
														$"GetSecCodeInfoModelByXmlText error: Expired date not recognized!");
													return null;
												}
											}
										}
									}
								}
							}
						}
					}
				}


				// check result has data
				if (secCodeInfo.SecCode is null ||
					secCodeInfo.Name is null ||
					secCodeInfo.FullName is null ||
					secCodeInfo.ISIN is null)
				{
					_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WebData " +
						$"GetSecCodeInfoModelByXmlText error: not all fields of model recognized.");
					return null;
				}

				if (secCodeInfo.SecBoard == 2)
				{
					// any bonds must have ISIN in SecBoard. 
					secCodeInfo.SecCode = secCodeInfo.ISIN;
				}

				return secCodeInfo;
			}

			_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WebData " +
				$"GetSecCodeInfoModelByXmlText error: some error of XML recognized");
			return null;
		}

		private string? GetSecCodeByXmlText(string xmlWithSecCode)
		{
			XmlDocument moexXml = new XmlDocument();
			moexXml.LoadXml(xmlWithSecCode);

			XmlElement? xRoot = moexXml.DocumentElement;
			if (xRoot is not null)
			{
				foreach (XmlElement xnode in xRoot)
				{
					foreach (XmlNode childNode in xnode.ChildNodes)
					{
						if (childNode.Name == "rows")
						{
							XmlNodeList? listOfRow = childNode.SelectNodes("row");
							if (listOfRow is null)
							{
								_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WebData GetSecCodeByXmlText " +
                                    $"XML doc has no items 'row'.");
								return null;
							}
							if (listOfRow.Count != 1)
							{
								_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WebData GetSecCodeByXmlText " +
                                    $"XML doc has too many items 'row'");
								return null;
							}

							if (listOfRow[0].Name == "row" && listOfRow[0].Attributes is not null)
							{
								XmlAttribute? attribute = listOfRow[0].Attributes["secid"];

								if (attribute is not null)
								{
									return attribute.Value;
								}
							}
						}
					}
				}
			}

			_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WebData GetSecCodeByXmlText " +
                $"can't recognize seccode from XML doc");
			return null;
		}
	}
}