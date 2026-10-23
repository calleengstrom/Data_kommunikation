using RpiApp.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace RpiApp.Services
{
    public class MeasurementCache
    {
        private readonly ConcurrentDictionary<string, RequestModel> _cacheMeasurements = new();


        public void UpdateCacheMeasuremnets(RequestModel request)
        {
            _cacheMeasurements[request.DeviceId] = request;

        }

        public List<RequestModel> GetAllCacheMeasurements()
        {
            return _cacheMeasurements.Values.ToList();
        }



    }
}
