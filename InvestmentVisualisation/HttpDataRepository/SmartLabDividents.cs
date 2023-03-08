using DataAbstraction.Interfaces;
using DataAbstraction.Models.BaseModels;
using DataAbstraction.Models.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;

namespace HttpDataRepository
{
    public class SmartLabDividents : ISmartLabDividents
    {
        private readonly ILogger<SmartLabDividents> _logger;
        private readonly SmartLabDiviPageSettings _webSettings;

        public SmartLabDividents(ILogger<SmartLabDividents> logger, IOptions<SmartLabDiviPageSettings> webSettings)
        {
            _logger = logger;
            _webSettings=webSettings.Value;
        }

        public async Task<List<SecCodeAndDividentModel>> GetDividentsTable()
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} SmartLabDividents SmartLabDividents called, " +
                $"baseURL={_webSettings.BaseUrl}");
            List<SecCodeAndDividentModel> result = new List<SecCodeAndDividentModel>();
            string rawHtmlSource = string.Empty;

            using (var client = new HttpClient(new HttpClientHandler 
                { 
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate 
                }))
            {
                //https://smart-lab.ru/dividends/yield/
                try
                {
                    HttpResponseMessage response = client.GetAsync(_webSettings.BaseUrl).Result;
                    response.EnsureSuccessStatusCode();
                    rawHtmlSource = response.Content.ReadAsStringAsync().Result;
                    _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} SmartLabDividents SmartLabDividents " +
                        $"rawHtmlSource lenght is {rawHtmlSource.Length}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} SmartLabDividents SmartLabDividents Exception at " +
                        $"baseURL={_webSettings.BaseUrl}, Message is: {ex.Message}");
                }

            }

            if (rawHtmlSource.Length > 0 && rawHtmlSource.Contains(_webSettings.StartWord))
            {
                //simple-little-table trades-table events moex_bonds_inline
                int startIndex = rawHtmlSource.IndexOf(_webSettings.StartWord);
                int endIndex = rawHtmlSource.Substring(startIndex).IndexOf(_webSettings.EndWord);

                string rawDataTable = rawHtmlSource.Substring(startIndex, endIndex);
                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} SmartLabDividents SmartLabDividents splitted text lenght is {rawDataTable.Length}");

                /*
                 <tr>
                    <td><a href="/forum/MRKP">Россети Центр и Приволжье</a></td>
                    <td>MRKP</td>
                    <td>27,9%</td>
                </tr>
                */
                string[] rawDataTableSplitted = rawDataTable.Split(_webSettings.TableRowSplitter);
                for (int i = 2; i < rawDataTableSplitted.Length; i++)//начинаем с 2 т.к. сначала идет огрызок от табле и хеадер
                {
                    string[] dataTr = rawDataTableSplitted[i].Split(_webSettings.TableCellSplitter);
                    SecCodeAndDividentModel newDiv = new SecCodeAndDividentModel();

                    //начинаем с 2 т.к. сначала идет остаток от tr, потом ссылка и имя
                    newDiv.SecCode = CleanTextFromHtmlTags(dataTr[2]);
                    newDiv.Divident = CleanTextFromHtmlTags(dataTr[3]);
                    
                    _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} SmartLabDividents SmartLabDividents " +
                        $"Add SecCodeAndDividentModel='{newDiv.SecCode}' '{newDiv.Divident}'");
                    result.Add(newDiv);
                }
            }
            else
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} SmartLabDividents SmartLabDividents rawHtmlSource " +
                    $"lenght is {rawHtmlSource.Length} or marker word '{_webSettings.StartWord}' not finded");
            }

            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} SmartLabDividents SmartLabDividents " +
                $"result list count is {result.Count}");
            return result;
        }

        private string CleanTextFromHtmlTags(string rawText)
        {
            foreach (string removeThis in _webSettings.CleanWordsFromCell)
            {
                rawText = rawText.Replace(removeThis, "");
            }

            return rawText;
        }
    }
}