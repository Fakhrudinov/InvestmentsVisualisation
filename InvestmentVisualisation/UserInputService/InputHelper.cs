using DataAbstraction.Interfaces;
using Microsoft.Extensions.Logging;

namespace UserInputService
{
    public class InputHelper
    {
        private IMySqlSecCodesRepository _secCodesRepo;
        private readonly ILogger<InputHelper> _logger;

        public InputHelper(
            ILogger<InputHelper> logger,
            IMySqlSecCodesRepository secCodesRepo)
        {
            _logger = logger;
            _secCodesRepo = secCodesRepo;
        }

        public bool IsDataFormatCorrect(string text)
        {
            if(DateTime.TryParse(text, out var dateTime))
            {
                return true;
            }
            return false;
        }

        public bool IsDecimal(string text)
        {
            string textCleaned = CleanPossibleNumber(text);
            //31000
            //  31 000,00
            //60,009.00

            // now only 31000 or 31000.11 possible. Or some shit...
            if (Decimal.TryParse(textCleaned, out decimal parsedWithDot))
            {
                return true;
            }
            else if (Decimal.TryParse(textCleaned.Replace(".", ","), out decimal parsedWithComma))
            {
                return true;
            }

            return false;
        }
        public decimal GetDecimalFromString(string text)
        {
            string textCleaned = CleanPossibleNumber(text);

            if (Decimal.TryParse(textCleaned, out decimal parsedWithDot))
            {
                return parsedWithDot;
            }
            else if (Decimal.TryParse(textCleaned.Replace(".", ","), out decimal parsedWithComma))
            {
                return parsedWithComma;
            }

            return -1;
        }


        // default delimeter is point = '.'
        public string CleanPossibleNumber(string text)
        {
            text = text.Replace(" ", "");
            int dotIndex = text.IndexOf('.');
            int commaIndex = text.IndexOf(",");

            if (dotIndex > 0 && commaIndex  > 0)
            {
                // if . is correct
                if (dotIndex > commaIndex)
                {
                    text = text.Replace(",", "");
                    return text;
                }
                else
                {
                    text = text.Replace(".", "");
                    text = text.Replace(",", ".");
                    return text;
                }
            }
            else if(dotIndex < 0 && commaIndex < 0)
            {
                // no delimeters
                return text;
            }
            else
            {
                // if no .
                if (dotIndex < 0)
                {
                    text = text.Replace(",", ".");
                    return text;
                }
                else
                {
                    return text;
                }
            }
        }

        public string[]? ReturnSplittedArray(string text)
        {
            while (text.Contains("\t\t"))
            {
                text = text.Replace("\t\t", "\t");
            }

            string[] textSplitted = text.Split("\t");
            if (textSplitted.Length < 3)
            {
                return null;
            }

            //fix - remove empty cell like this: '\t '
            List<string> cleaned = new List<string>();
            foreach (string cell in textSplitted)
            {
                string newcell = cell.Trim();

                if (newcell.Length > 0)
                {
                    cleaned.Add(newcell);
                }
            }
            
            return cleaned.ToArray();
        }

        public async Task<string> GetSecCodeByISIN(CancellationToken cancellationToken, string text)
        {
            //ТМК, ПАО ао01; ISIN-RU000A0B6NK6;
            string isin = text.Split("ISIN-")[1];
            if (isin.Length < 12)
            {
                return "Ошибка. Длинна ISIN слишком мала: " + isin;
            }
            isin = isin.Substring(0, 12);

            string secCode = await _secCodesRepo.GetSecCodeByISIN(cancellationToken, isin);
            _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} InputHelper GetSecCodeByISIN " +
                $"получили из репозитория={secCode}");

            return secCode;
        }

        public bool IsInt32Correct(string text)
        {
            //31000
            //31 000,00
            //31,000.00
            string textCleaned = CleanPossibleNumber(text);
            
            // now only 31000 or 31000.11 possible.
            string textCleanedWitoutTail = textCleaned.Replace(".00", "");

            // now only 31000 possible. Or some shit...
            if (Int32.TryParse(textCleanedWitoutTail, out int parsedWithDot))
            {
                return true;
            }
            //else if (Int32.TryParse(textCleanedWitoutTail.Replace(".", ","), out int parsedWithComma))
            //{
            //    return true;
            //}

            return false;
        }
        public int GetInt32FromString(string text)
        {
            string textCleaned = CleanPossibleNumber(text);

            // now only 31000 or 31000.11 possible.
            string textCleanedWitoutTail = textCleaned.Replace(".00", "");

            // now only 31000 possible. Or some shit...
            if (Int32.TryParse(textCleanedWitoutTail, out int parsedWithDot))
            {
                return parsedWithDot;
            }

            return -1;
        }
    }
}
