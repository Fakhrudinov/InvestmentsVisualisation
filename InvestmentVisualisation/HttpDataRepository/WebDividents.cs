using DataAbstraction.Interfaces;
using DataAbstraction.Models.BaseModels;
using DataAbstraction.Models.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;


namespace HttpDataRepository
{
    public class WebDividents : IWebDividents
    {
        private readonly ILogger<WebDividents> _logger;
        private readonly IOptionsSnapshot<WebDiviPageSettings> _namedOptions;
        private WebDiviPageSettings _options;

        //private readonly WebDiviPageSettings _smLabOptions;
        //private readonly WebDiviPageSettings _invLabOptions;
        //private readonly WebDiviPageSettings _dohodOptions;
        //private readonly WebDiviPageSettings _vsdelkeOptions; 

        public WebDividents(
            ILogger<WebDividents> logger, 
            IOptionsSnapshot<WebDiviPageSettings> namedOptions)
        {
            _logger = logger;
            _namedOptions = namedOptions;
            //_smLabOptions=namedOptions.Get("SmLab");
            //_invLabOptions=namedOptions.Get("InvLab");
            //_dohodOptions=namedOptions.Get("Dohod");
            //_vsdelkeOptions=namedOptions.Get("Vsdelke");
        }

        public DohodDivsAndDatesModel? GetDividentsTableFromDohod()
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WebDividents GetDividentsTableFromDohod called");

            _options=_namedOptions.Get("Dohod");

            //download html and get table with dividents
            string[]? rawDataTableSplitted = DownloadAndGetHtmlTableWithDividents();
            if (rawDataTableSplitted is null)
            {
                return null;
            }
            DohodDivsAndDatesModel result = new DohodDivsAndDatesModel();
            result.DohodDivs = GetDividentsFromHtml(rawDataTableSplitted);
            result.DohodDates = GetDividentsDatesFromHtml(rawDataTableSplitted);

            return result;
        }

        public List<SecCodeAndDividentModel>? GetDividentsTableFromVsdelke()
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WebDividents GetDividentsTableFromVsdelke called");

            _options=_namedOptions.Get("Vsdelke");
            return GetDividents();
        }
        public List<SecCodeAndDividentModel>? GetDividentsTableFromSmartLab()
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WebDividents GetDividentsTableFromSmartLab called");

            _options=_namedOptions.Get("SmLab");
            return GetDividents();
        }
        public List<SecCodeAndDividentModel>? GetDividentsTableFromInvLab()
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WebDividents GetDividentsTableFromInvLab called");

            _options=_namedOptions.Get("InvLab");
            return GetDividents();
        }
        private List<SecCodeAndDividentModel> ? GetDividents()
        {
            string[] ? rawDataTableSplitted = DownloadAndGetHtmlTableWithDividents();
            if (rawDataTableSplitted is null)
            {
                return null;
            }

            ////get fool html source
            //string ? rawHtmlSource = GetFullHtmlPage(options.BaseUrl);

            //if (rawHtmlSource is null || !rawHtmlSource.Contains(options.StartWord) || !rawHtmlSource.Contains(options.EndWord))
            //{
            //    _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WebDividents GetDividents " +
            //        $"rawHtmlSource is null " +
            //        $"or StartWord pointer '{options.StartWord}' is not found " +
            //        $"or EndWord pointer '{options.EndWord}' is not found!");

            //    return null;
            //}

            ////cut table from full html source
            //int startIndex = rawHtmlSource.IndexOf(options.StartWord);
            //int endIndex = rawHtmlSource.Substring(startIndex).IndexOf(options.EndWord);

            //string rawDataTable = rawHtmlSource.Substring(startIndex, endIndex);
            //_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WebDividents GetDividents splitted text lenght is {rawDataTable.Length}");

            ////split
            //string[] rawDataTableSplitted = rawDataTable.Split(options.TableRowSplitter);

            //заполним дивиденты
            List<SecCodeAndDividentModel> result = GetDividentsFromHtml(rawDataTableSplitted);

            //List<SecCodeAndDividentModel> result = new List<SecCodeAndDividentModel>();
            //for (int i = _options.NumberOfRowToStartSearchData; i < rawDataTableSplitted.Length; i++)
            //{
            //    string[] dataTr = rawDataTableSplitted[i].Split(_options.TableCellSplitter);
            //    SecCodeAndDividentModel newDiv = new SecCodeAndDividentModel();

            //    // for vsdelke.ru - alternative get SecCode method - from <font class="tiker">BELU</font>
            //    if (_options.BaseUrl.Contains("vsdelke.ru"))
            //    {
            //        newDiv.SecCode = GetSecCodeFromVsdelkeTD(dataTr[_options.NumberOfCellWithHref].Replace("\"", "'"));
            //    }
            //    else // other contain seccode in href
            //    {
            //        newDiv.SecCode = GetSecCodeFromTD(dataTr[_options.NumberOfCellWithHref].Replace("\"", "'"));
            //    }
                
            //    if (newDiv.SecCode is null)
            //    {
            //        //не найден href
            //        continue;
            //    }

            //    // debug point
            //    //if (newDiv.SecCode.Contains("AKRN"))
            //    //{
            //    //    Console.WriteLine();
            //    //}

            //    newDiv.Divident = CleanTextFromHtmlTags(dataTr[_options.NumberOfCellWithDiscont].Replace("\"", "'"), _options.CleanWordsFromCell);
            //    if (newDiv.Divident.Equals("0%") || 
            //        newDiv.Divident.Replace(",", ".").Equals("0.0%") ||
            //        newDiv.Divident.Replace(",", ".").Equals("0.00%"))
            //    {
            //        // не добавляем пустые
            //        continue;
            //    }

            //    _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WebDividents GetDividents " +
            //        $"Add SecCodeAndDividentModel='{newDiv.SecCode}' '{newDiv.Divident}'");

            //    /// пробуем найти, если нет - добавляем.
            //    /// если есть, сравниваем, одинаковые ПРОПУСКАЕМ
            //    /// если меньше, чем есть - ПРОПУСКАЕМ
            //    /// если больше - заменяем существуещее значение
            //    int index = result.FindIndex(res => res.SecCode == newDiv.SecCode);
            //    if (index >= 0)
            //    {
            //        if (result[index].Divident.Equals(newDiv.Divident))
            //        {
            //            // ПРОПУСКАЕМ одинаковые
            //            continue;
            //        }
            //        else
            //        {
            //            bool biggerThenExist = CompareTwoDividentsValues(result[index].Divident, newDiv.Divident);
            //            if (biggerThenExist )
            //            {
            //                //если больше - заменяем существуещее значение
            //                result[index].Divident = newDiv.Divident;
            //            }
            //        }
            //    }
            //    else
            //    {
            //        // не нашли, добавляем
            //        result.Add(newDiv);
            //    }                
            //}

            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WebDividents GetDividents " +
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
                // debug point
                //if (newDiv.SecCode.Contains("AKRN"))
                //{
                //    Console.WriteLine();
                //}

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

                _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WebDividents GetDividents " +
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

        private string[]? DownloadAndGetHtmlTableWithDividents()
        {
            //get fool html source
            string? rawHtmlSource = GetFullHtmlPage(_options.BaseUrl);

            if (rawHtmlSource is null || !rawHtmlSource.Contains(_options.StartWord) || !rawHtmlSource.Contains(_options.EndWord))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WebDividents DownloadAndGetHtmlTableWithDividents " +
                    $"rawHtmlSource is null " +
                    $"or StartWord pointer '{_options.StartWord}' is not found " +
                    $"or EndWord pointer '{_options.EndWord}' is not found!");

                return null;
            }

            //cut table from full html source
            int startIndex = rawHtmlSource.IndexOf(_options.StartWord);
            int endIndex = rawHtmlSource.Substring(startIndex).IndexOf(_options.EndWord);

            string rawDataTable = rawHtmlSource.Substring(startIndex, endIndex);
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WebDividents DownloadAndGetHtmlTableWithDividents " +
                $"splitted text lenght is {rawDataTable.Length}");

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
            //<a href="https://invlab.ru/akcii/rossiyskie/bspb/"> // " class='action_title' data-val='Селигдар'><a href='https://invlab.ru/akcii/rossiyskie/selg/'>Селигдар</a></td>\n"
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

        private string? GetFullHtmlPage(string baseUrl)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WebDividents GetFullHtmlPage called, " +
                $"baseURL={baseUrl}");

            string rawHtmlSource = string.Empty;

            using (var client = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            }))
            {
                try
                {
                    HttpResponseMessage response = client.GetAsync(baseUrl).Result;
                    response.EnsureSuccessStatusCode();
                    rawHtmlSource = response.Content.ReadAsStringAsync().Result;

                    _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WebDividents GetFullHtmlPage " +
                        $"rawHtmlSource lenght is {rawHtmlSource.Length}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WebDividents GetFullHtmlPage Exception at " +
                        $"baseURL={baseUrl}, Message is: {ex.Message}");

                    return null;
                }
            }

            return rawHtmlSource;
        }
    }
}