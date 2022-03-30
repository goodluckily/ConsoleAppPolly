using System;
using System.Net.Http;
using System.Threading;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace ConsoleAppPolly
{
    public class Program
    {
        private static TimeoutPolicy _timeoutPolicy;
        static void Main(string[] args)
        {
            //try
            //{
            //    _timeoutPolicy = Policy.Timeout(20, TimeoutStrategy.Pessimistic).re;
            //    var aa = _timeoutPolicy.Execute(() => GetTest());
            //    Console.WriteLine(aa);
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex);
            //}


            //var retryPolicy = HttpPolicyExtensions
            //                .HandleTransientHttpError() // HttpRequestException, 5XX and 408
            //                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(retryAttempt));


            #region 1.降级 以及 获取异常
            //Policy policy = Policy.Handle<ArgumentException>()  //故障1 类型转换失败
            //                      .Or<ArgumentNullException>()  //故障2 null
            //                      .Or<IndexOutOfRangeException>()     //故障3
            //                      .Fallback(x =>
            //                      {
            //                          //步奏2
            //                          Console.WriteLine("我是降级后的执行的操作 x");
            //                      }, ex =>
            //                      {
            //                          //步奏1
            //                          Console.WriteLine($"业务报错信息为：{ex.Message}");
            //                      });

            //policy.Execute(() =>
            //{
            //    //执行业务代码
            //    Console.WriteLine("开始任务");
            //    throw new ArgumentNullException("类型转换失败");
            //    Console.WriteLine("结束任务");
            //}); 
            #endregion


            #region 2.降级-获取返回值

            //Policy<string> policy = Policy<string>.Handle<ArgumentException>()  //故障
            //                         .Fallback(() =>
            //                         {
            //                             //降级执行的动作
            //                             Console.WriteLine("我是降级后的执行的操作");
            //                             return "我是降级业务中的返回值";
            //                         });

            //string value = policy.Execute(() =>
            //{
            //    //执行业务代码
            //    Console.WriteLine("开始任务");
            //    throw new ArgumentException("类型转换失败");
            //    Console.WriteLine("结束任务");
            //    return "我是正常业务中的返回值";
            //});

            //Console.WriteLine($"最终结果为：{value}");

            #endregion


            #region 3.熔断机制

            //下面设置的是连续出错3次之后熔断10秒，意思是：连续出错3次后，熔断10s，在这10s内，再次访问，不再执行Execute中的代码，直接报错，
            //10s熔断时间过后，继续访问，如果还是出错(出一次即可)，直接熔断10s， 再次重复这个过程
            //Policy policy = Policy
            // .Handle<Exception>()
            // .CircuitBreaker(3, TimeSpan.FromSeconds(10));    //连续出错3次之后熔断10秒(不会再去尝试执行业务代码）。 
            //while (true)
            //{
            //    Console.WriteLine("开始Execute");
            //    try
            //    {
            //        policy.Execute(() =>
            //        {
            //            Console.WriteLine("-------------------------------------开始任务---------------------------------------");
            //            throw new Exception();
            //            Console.WriteLine("完成任务");
            //        });
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine("execute出错" + ex.Message);
            //    }
            //    Thread.Sleep(2000);
            //}
            #endregion

            #region 4.重试机制

            //try
            //{
            //    Policy policy1 = Policy
            //    .Handle<Exception>()
            //    //.Retry(3); //Retry 出错后,连续执行3次
            //    //.RetryForever();//出错后,连续执行,直到成功为止
            //    .WaitAndRetry(5, i => TimeSpan.FromSeconds(10));  //重试5次，每次间隔10s
            //    int g = 0;
            //    policy1.Execute(() =>
            //    {
            //        Console.WriteLine($"开始任务,g={g}");
            //        if (g < 10)
            //        {
            //            g++;
            //            throw new Exception("业务出错了");
            //        }

            //        Console.WriteLine("完成任务");
            //    });
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine($"捕获异常:{ex.Message}");
            //}

            #endregion

            #region 5.组合机制

            //使用Wrap包裹，eg：policy6 = Policy.Wrap(policy1, policy2)
            //注意：Wrap是有包裹顺序的，内层的故障如果没有被处理则会抛出到外层.

            #region (1).超时3秒降级

            ////1.1 超时3秒
            //Policy policytimeout = Policy.Timeout(3, TimeoutStrategy.Pessimistic);
            ////1.2 降级
            //Policy policyFallBack = Policy.Handle<TimeoutRejectedException>()
            //   .Fallback(() =>
            //   {
            //       //降级执行的动作
            //       Console.WriteLine("我是降级后的执行的操作");
            //   }, ex =>
            //   {
            //       //捕获业务中的出错信息
            //       Console.WriteLine(ex.Message);
            //   });
            ////1.3 将超时和降级操作进行组合
            //Policy policy = policyFallBack.Wrap(policytimeout);
            ////1.4 执行业务代码
            //policy.Execute(() =>
            //{
            //    Console.WriteLine("开始任务");
            //    Thread.Sleep(5000);
            //    Console.WriteLine("完成任务");
            //});

            #endregion


            #region (2).重试+降级：重试3次,期间成功,则继续执行后面业务;期间失败,则走外层的降级操作

            ////2.1 遇到异常重试3次
            //Policy policyRetry = Policy.Handle<Exception>().Retry(3);
            ////2.2 降级操作
            //Policy policyFallback = Policy.Handle<Exception>()
            //                              .Fallback(() =>
            //                              {
            //                                  //降级执行的动作
            //                                  Console.WriteLine("我是降级后的执行的操作");
            //                              }, ex =>
            //                              {
            //                                  //捕获业务中的出错信息
            //                                  Console.WriteLine(ex.Message);
            //                              });
            ////Wrap：包裹。policyRetry在里面，policyFallback在外面，如果里面出现了故障，则把故障抛出来给外面
            ////2.3 进行包裹(出现错误,先重试3次,期间成功,则继续执行后面业务;期间失败,则走外层的降级操作)
            //Policy policy = policyFallback.Wrap(policyRetry);
            //int g = 0;
            ////2.4 执行业务代码
            //policy.Execute(() =>
            //{
            //    Console.WriteLine($"开始任务,g={g}");
            //    if (g < 10)
            //    {
            //        g++;
            //        throw new Exception("业务出错了");
            //    }

            //    Console.WriteLine("完成任务");
            //});


            #region (3).熔断+降级：Execute执行业务代码无须再用Try-catch包裹，否则不抛异常，则无法降级，我们这里演示的是降级，并在降级中拿到业务代码的异常信息




            #endregion

            #endregion

            #region (3).熔断+降级：Execute执行业务代码无须再用Try-catch包裹，否则不抛异常，则无法降级，我们这里演示的是降级，并在降级中拿到业务代码的异常信息

            ////3.1 熔断
            //Policy policyCBreaker = Policy.Handle<Exception>()
            //                        .CircuitBreaker(3, TimeSpan.FromSeconds(10));    //连续出错3次之后熔断10秒(不会再去尝试执行业务代码）。 
            //                                                                         //3.2 降级
            //Policy policyFallback = Policy.Handle<Exception>()
            //                       .Fallback(() =>
            //                       {
            //                           //降级执行的动作
            //                           Console.WriteLine("我是降级后的执行的操作");
            //                       }, ex =>
            //                       {
            //                           //这里是捕获业务代码中的错误，业务代码中就不要再写try-catch，否则不抛异常，则无法降级
            //                           Console.WriteLine($"业务报错信息为：{ex.Message}");
            //                       });
            ////3.4 包裹
            //Policy policy = policyFallback.Wrap(policyCBreaker);

            ////3.4 执行业务
            //while (true)
            //{
            //    Console.WriteLine("开始Execute");
            //    //try
            //    //{
            //    policy.Execute(() =>
            //    {
            //        Console.WriteLine("-------------------------------------开始任务---------------------------------------");
            //        throw new Exception();
            //        Console.WriteLine("完成任务");
            //    });
            //    //}
            //    // 不要再写try-catch，否则不抛异常，则无法降级
            //    //catch (Exception ex)
            //    //{
            //    //    Console.WriteLine("execute出错" + ex.Message);
            //    //}
            //    Thread.Sleep(2000);
            //}

            #endregion

            #endregion

            Console.ReadKey();
        }

        public static string GetTest()
        {
            var httpClient = new HttpClient();
            var aa = httpClient.GetStringAsync("http://192.168.6.55:9527/Role/GetRoleListByPage").Result;
            return aa;
        }
    }
}
