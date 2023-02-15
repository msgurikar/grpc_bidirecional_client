using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SampleClient;
using SampleService;
using Grpc.Core;
using System.Collections.Concurrent;

namespace SampleGrpcBiDirection
{
    public class SampleClientProviderImpl : SampleClientService.SampleClientServiceBase
    {
        private string serviceAddress = string.Empty;
        ConcurrentDictionary<Guid, bool> processCompletionStatus = new ConcurrentDictionary<Guid, bool>();
        protected static bool cancelRequested;

        public override async Task<SampleClient.Response> Compute(SampleClient.Request request, ServerCallContext context)
        {
            var num_requests = request.NumRequests;
            Console.WriteLine($"Compute request recieved with number of requests is {num_requests}");

            serviceAddress = Environment.GetEnvironmentVariable("MY_SERVICE_TARGET");
            var computeTasks = new List<Task<Tuple<ResponseStatus, string>>>();
            string msg;
            StringBuilder errorMsg = new StringBuilder();
            Tuple<ResponseStatus, string>[] results = null;
            try
            {
                for (int i=0;i<num_requests;i++)
                {
                    var num = i;
                    computeTasks.Add(Task<Tuple<ResponseStatus, string>>.Run(async () =>
                    {
                                            
                        var responseTuple = await InvokeService(num);
                        if (responseTuple.Item1 == ResponseStatus.Success)
                        {
                            Console.WriteLine($"{num} completed success");
                            return Tuple.Create(ResponseStatus.Success, responseTuple.Item2);
                        }
                        else if (responseTuple.Item1 == ResponseStatus.Failed)
                        {                           
                            msg = $"Error occured while computing  {num}";
                            errorMsg.AppendLine(msg);
                            Console.WriteLine(errorMsg);

                            return Tuple.Create(ResponseStatus.Failed, responseTuple.Item2);
                        }
                        else if (responseTuple.Item1 == ResponseStatus.Cancelled)
                        {
                           
                            msg = $"{num} Computation cancelled by user..";
                            errorMsg.AppendLine(msg);
                            Console.WriteLine(errorMsg);
                            return Tuple.Create(ResponseStatus.Cancelled, responseTuple.Item2);
                        }
                        else
                        {
                            return Tuple.Create(ResponseStatus.Failed, "Unknown");
                        }
                    }));
                }

                var computeTask = Task.WhenAll(computeTasks.ToArray()); // Waiting for response from all tasks.

                results = await computeTask;
               
                foreach (var res in results)
                {
                    if (res.Item1 == ResponseStatus.Failed)
                    {                       
                        Console.WriteLine($"One or more Compute tasks have been failed due to {res.Item2}");
                        throw new Exception(res.Item2);
                    }                 
                }
                Console.WriteLine("All tasks have been completed");                                            

               
            }
            catch (AggregateException ex)
            {
               
                foreach (var innerex in ex.InnerExceptions)
                {
                    msg = $"Inner Exception is {innerex.Message}";
                    errorMsg.AppendLine(msg);
                    Console.WriteLine(errorMsg);
                }
               
            }                        
       
            var resp = new SampleClient.Response();
            return resp;
        }

        private async Task<Tuple<ResponseStatus, string>> InvokeService(int num)
        {
            try
            {
                var channel = new Channel($"{serviceAddress}", ChannelCredentials.Insecure);
                var client = new SampleService.SampleService.SampleServiceClient(channel);

                var compute = client.Compute();
                var responseTuple = await ReadResponseAndWriteRequestAsync(compute, num); // Waits for read and write operation.
                compute.Dispose();

                return responseTuple;
            }
            catch(Exception ex)
            {
                Console.WriteLine($"SampleService compute failed {ex.Message}");
                return(Tuple.Create(ResponseStatus.Failed, ex.Message));
            }
        }



        private async Task<Tuple<ResponseStatus, string>> ReadResponseAndWriteRequestAsync(
            AsyncDuplexStreamingCall<SampleService.Request, SampleService.Response> stream,
            int num)
        {
            var id = Guid.NewGuid();
            processCompletionStatus.TryAdd(id, false);
            var writeTask = WriteRequestAsync(stream, num, id);
            var readtask = ReadResponseAsync(stream);
            await readtask;
            processCompletionStatus[id] = true;
            await writeTask;
            return readtask.Result;
        }


        private async Task<Tuple<ResponseStatus, string>> ReadResponseAsync(
                AsyncDuplexStreamingCall<SampleService.Request, SampleService.Response> stream)
        {
            var response = new SampleService.Response();
            try
            {
                while (await stream.ResponseStream.MoveNext())
                {

                    var resp = stream.ResponseStream.Current;
                    if (string.IsNullOrEmpty(resp.Message))
                        continue;

                    if (resp.Status == SampleService.ResponseStatus.InProgress)
                    {
                        continue;
                    }

                    if (resp.Status == SampleService.ResponseStatus.Success)
                    {                    
                        return Tuple.Create(ResponseStatus.Success, $"Pod Name is {resp.PodName} and Pod Status is {resp.PodStatus} and message is {resp.Message}");
                    }
                    else if (resp.Status == SampleService.ResponseStatus.Failed)
                    {                 
                        return Tuple.Create(ResponseStatus.Failed, resp.Message);
                    }
                    else if (resp.Status == SampleService.ResponseStatus.Cancelled)
                    {                    
                        return Tuple.Create(ResponseStatus.Cancelled, resp.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                return Tuple.Create(ResponseStatus.Failed, ex.Message);
            }

            return Tuple.Create(ResponseStatus.Failed, "Unknown issue"); ;
        }


        private async Task WriteRequestAsync(
            AsyncDuplexStreamingCall<SampleService.Request, SampleService.Response> stream, int num, Guid msgId)
        
        {
                var request = new SampleService.Request();
                var input_msg = $"Message is {num}";
                request.InputMsg = input_msg;
                await stream.RequestStream.WriteAsync(request);

                while (true)
                {
                    try
                    {
                        if (cancelRequested)
                        {
                           Console.WriteLine("Sending cancel request to service..");
                            var cancelRequest = request.Clone();
                            cancelRequest.RequestCancel = true;
                            await stream.RequestStream.WriteAsync(cancelRequest);
                            break;
                        }
                        else
                        {
                             await stream.RequestStream.WriteAsync(request);
                        }
                        await Task.Delay(100);
                    }
                    catch (Exception)
                    {
                        break;
                    }

                    if (processCompletionStatus.TryGetValue(msgId, out bool completed) && completed)
                    {
                        // _logger.Log(LogLevel.Info, $"Process completed {msgId}");
                        Console.WriteLine("WriteRequest completed success.");
                        return;
                    }
                }
        }
    }    
}
