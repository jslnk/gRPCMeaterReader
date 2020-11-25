using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MeterReaderWeb.Data;
using MeterReaderWeb.Data.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MeterReaderWeb.Services
{
    public class MeterService : MeterReadingService.MeterReadingServiceBase
    {
        private readonly ILogger<MeterService> _logger;
        private readonly IReadingRepository _repository;

        public MeterService(ILogger<MeterService> logger, IReadingRepository repository)
        {
            _logger = logger;

            _repository = repository;
        }


        public override async Task<Empty> SendDiagnostics(IAsyncStreamReader<ReadingMessage> requestStream, ServerCallContext context)
        {
            var theTask = Task.Run(async () =>
            {
                await foreach (var reading in requestStream.ReadAllAsync())
                {
                    _logger.LogInformation($"Received Reading : {reading}");
                }
            });

            await theTask;
            return new Empty();
        }


        public async override Task<StatusMessage> AddReading(ReadingPacket request, ServerCallContext context)
        {
         
            var result = new StatusMessage()
            {
                Success = ReadingStatus.Failure
            };

            if(request.Successful == ReadingStatus.Success)
            {
                try 
                {
                    foreach (var r in request.Readings)
                    {
                        //save to the DB
                        var reading = new MeterReading()
                        {
                            Value = r.ReadingValue,
                            ReadingDate = r.ReadingTime.ToDateTime(),
                            CustomerId = r.CustomerId
                        };

                        _repository.AddEntity(reading);
                    }

                    if(await _repository.SaveAllAsync())
                    {
                        _logger.LogInformation($"Stored {request.Readings.Count} New Readings...");
                        result.Success = ReadingStatus.Success;
                    }
                }
                catch(Exception ex)
                {
                    result.Message = "Exception thrown during process";
                    _logger.LogError($"Exception thrown during saving of readings:{ex}");
                }
               
            }

            //return Task.FromResult(result); since using async, doesn't need to Task.FromResult
            return result;
                        
        }
    }
}
