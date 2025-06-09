using DataAbstraction.Interfaces;
using DataAbstraction.Models.Deals;
using DataAbstraction.Models.Incoming;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using UserInputService;

namespace InMemoryRepository
{
    public class Repository : IInMemoryRepository
    {
        private readonly ILogger<Repository> _logger;
        private ConcurrentDictionary<Guid, IndexedDealModel> _dictionaryDeals;
		private ConcurrentDictionary<Guid, IndexedIncomingModel> _dictionaryIncomings;
		private InputHelper _helper;

        public Repository(ILogger<Repository> logger, InputHelper helper)
        {
            _dictionaryDeals = new ConcurrentDictionary<Guid, IndexedDealModel>();
			_dictionaryIncomings = new ConcurrentDictionary<Guid, IndexedIncomingModel>();
			_logger = logger;
            _helper = helper;
        }

        public void DeleteAllDeals()
        {
            _dictionaryDeals.Clear();
        }

        public void AddNewDeal(CreateDealsModel newDeal)
        {
            IndexedDealModel newDictionaryDeal = new IndexedDealModel
            {
                Id = Guid.NewGuid(),
                SecBoard = newDeal.SecBoard,
                SecCode = newDeal.SecCode,
                AvPrice = newDeal.AvPrice,
                Pieces = newDeal.Pieces,
                NKD = newDeal.NKD,
                Comission = newDeal.Comission,
                Date = newDeal.Date
            };

            _dictionaryDeals.TryAdd(newDictionaryDeal.Id, newDictionaryDeal);
        }

        public List<IndexedDealModel> GetAllDeals()
        {
            return _dictionaryDeals.Values.ToList();
        }

        public int ReturnDealsCount()
        {
            return _dictionaryDeals.Count;
        }

        public void DeleteSingleDealByGuid(Guid guid)
        {
            _dictionaryDeals.TryRemove(guid, out IndexedDealModel someDeal);
        }
        public void DeleteSingleDealByStringId(string id)
        {
            try
            {
                Guid guid = new Guid(id);
                _dictionaryDeals.TryRemove(guid, out IndexedDealModel someDeal);
            }
            catch (System.FormatException)
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IInMemoryRepository " +
                $"DeleteSingleDeal error - GUID can't parse {id}");
            }
        }

        public void EditSingleDeal(IndexedDealModel editDeal)
        {
            _dictionaryDeals.TryGetValue(editDeal.Id, out IndexedDealModel ? someDeal);

            if (someDeal is not null)
            {
                editDeal.SecCode = someDeal.SecCode;
                editDeal.SecBoard = someDeal.SecBoard;

                _dictionaryDeals.TryUpdate(editDeal.Id, editDeal, someDeal);
            }            
        }

        public CreateDealsModel? GetCreateDealsModelByGuid(Guid guid)
        {
            _dictionaryDeals.TryGetValue(guid, out IndexedDealModel? someDeal);

            if (someDeal is not null)
            {
                CreateDealsModel result = new CreateDealsModel
                {
                    Comission = someDeal.Comission,
                    SecBoard = someDeal.SecBoard,
                    SecCode = someDeal.SecCode,
                    AvPrice = someDeal.AvPrice,
                    Pieces = someDeal.Pieces,
                    Date = someDeal.Date,
                    NKD = someDeal.NKD,
                };

                return result;
            }

            return null;
        }
        public CreateDealsModel? GetCreateDealsModelById(string id)
        {
            try
            {
                Guid guid = new Guid(id);
                return GetCreateDealsModelByGuid(guid);
            }
            catch (System.FormatException)
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IInMemoryRepository " +
                $"GetCreateDealsModelById error - GUID can't parse {id}");
            }

            return null;
        }

        public void CombineTwoDealsById(Guid targetDeal, Guid sourceDeal)
        {
            _dictionaryDeals.TryGetValue(targetDeal, out IndexedDealModel? findedTargetDeal);
            _dictionaryDeals.TryGetValue(sourceDeal, out IndexedDealModel? findedSourceDeal);

            if (findedTargetDeal is not null && findedSourceDeal is not null)
            {
                findedTargetDeal.Pieces = findedSourceDeal.Pieces + findedTargetDeal.Pieces;

                findedTargetDeal.Comission = CombineTwoStringAsDecimal(findedSourceDeal.Comission, findedTargetDeal.Comission);
                findedTargetDeal.NKD = CombineTwoStringAsDecimal(findedSourceDeal.NKD, findedTargetDeal.NKD);

                DeleteSingleDealByGuid(sourceDeal);
            }
        }

        private string? CombineTwoStringAsDecimal(string? stringOne, string? stringTwo)
        {
            // logic
            //      if not null both
            //          check if is decimal both
            //              then summ as decimal
            //      else summ as string
            if (
                (stringOne is not null && _helper.IsDecimal(stringOne)) &&
                (stringTwo is not null && _helper.IsDecimal(stringTwo))
                )
            {
                decimal one = _helper.GetDecimalFromString(stringOne);
                decimal two = _helper.GetDecimalFromString(stringTwo);
                return (one + two).ToString();
            }
            else
            {
                return _helper.CleanPossibleNumber(stringOne) + _helper.CleanPossibleNumber(stringTwo);
            }
        }

		public void AddNewIncoming(CreateIncomingModel newIncoming)
		{
			IndexedIncomingModel newDictionaryIncom = new IndexedIncomingModel
			{
				Id = Guid.NewGuid(),
				SecBoard = newIncoming.SecBoard,
				SecCode = newIncoming.SecCode,
				Category = newIncoming.Category,
				Value = newIncoming.Value,
				Comission = newIncoming.Comission,
				Date = newIncoming.Date,
                IsRecognized = newIncoming.IsRecognized,
			};

			_dictionaryIncomings.TryAdd(newDictionaryIncom.Id, newDictionaryIncom);
		}

		public void DeleteAllIncomings()
		{
			_dictionaryIncomings.Clear();
		}

		public void DeleteSingleIncomingByStringId(string id)
		{
			try
			{
				Guid guid = new Guid(id);
				_dictionaryIncomings.TryRemove(guid, out IndexedIncomingModel ? someIncom);
			}
			catch (System.FormatException)
			{
				_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IInMemoryRepository " +
				$"DeleteSingleIncomingByStringId error - GUID can't parse {id}");
			}
		}

		public List<IndexedIncomingModel>? GetAllIncomings()
		{
			return _dictionaryIncomings.Values.ToList(); 
		}

		public CreateIncomingModel? GetCreateIncomingModelByGuid(Guid guid)
		{
			_dictionaryIncomings.TryGetValue(guid, out IndexedIncomingModel? someIncom);

			if (someIncom is not null)
			{
                CreateIncomingModel result = new CreateIncomingModel
                {
                    Comission = someIncom.Comission,
                    SecBoard = someIncom.SecBoard,
                    SecCode = someIncom.SecCode,
                    Date = someIncom.Date,
                    Category = someIncom.Category,
                    Value = someIncom.Value,
                    IsRecognized = someIncom.IsRecognized,
                };
				return result;
			}

			return null;
		}

		public CreateIncomingModel? GetCreateIncomingModelById(string id)
		{
			try
			{
				Guid guid = new Guid(id);
				return GetCreateIncomingModelByGuid(guid);
			}
			catch (System.FormatException)
			{
				_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IInMemoryRepository " +
				$"GetCreateIncomingModelById error - GUID can't parse {id}");
			}

			return null;
		}

		public int ReturnIncomingsCount()
		{
			return _dictionaryIncomings.Count;
		}

		public void EditSingleIncoming(IndexedIncomingModel model)
		{
			_dictionaryIncomings.TryGetValue(model.Id, out IndexedIncomingModel? someDeal);

			if (someDeal is not null)
			{
				//model.SecCode = someDeal.SecCode;
				//model.SecBoard = someDeal.SecBoard;

				_dictionaryIncomings.TryUpdate(model.Id, model, someDeal);
			}
		}
	}
}
