using HMIUserApp.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMIUserApp.Service
{
    public class LogDataService
    {
        private readonly ApplicationDBContext _context;
        public LogDataService()
        {
            _context = new ApplicationDBContext();
            _context.Database.EnsureCreated();
        }
        public void AddRecord(ModbusData data)
        {
            _context.Add(data);
        }
        public void Save()
        {
            _context.SaveChangesAsync();
        }
        public Tuple<DateTime, DateTime> GetMinMaxDate()
        {
            var startDate = _context.ModbusDatas.OrderBy(x => x.DateTime).FirstOrDefault();
            var endDate = _context.ModbusDatas.OrderByDescending(x => x.DateTime).FirstOrDefault();
            return Tuple.Create(startDate != null ? startDate.DateTime : DateTime.Now, endDate != null ? endDate.DateTime: DateTime.Now);
        }
        public List<ModbusData> GetModbusDatas(string tag, DateTime startDate, DateTime endDate)
        {
            return _context.ModbusDatas.Where(x => x.TagName.StartsWith(tag) && x.DateTime >= startDate && x.DateTime <= endDate.AddDays(1)).OrderBy(x=> x.DateTime).ThenBy(y =>y.ModbusAddress).ToList();
        }
        public async Task DeleteRecord(DateTime startDate, DateTime endDate)
        {
            _context.ModbusDatas.RemoveRange(_context.ModbusDatas.Where(x => x.DateTime >= startDate && x.DateTime <= endDate.AddDays(1)));
            await _context.SaveChangesAsync();
        }
    }
}
