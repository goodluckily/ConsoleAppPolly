using System;
using Polly;
using Polly.Timeout;

namespace ConsoleAppPolly
{
    public class PollyHelper
    {
        /// <summary>
        /// 超时的方法设置
        /// </summary>
        /// <param name="action">执行的方法</param>
        /// <param name="seconds">秒</param>
        public static void PolicyTimeout(Action action, int seconds)
        {
            var _timeoutPolicy = Policy.Timeout(seconds, TimeoutStrategy.Pessimistic);
            _timeoutPolicy.Execute(() => action.Invoke());
        }

        /// <summary>
        /// 重试的调用方法设置
        /// </summary>
        /// <param name="action">执行的方法</param>
        /// <param name="Retry">重试次数</param>
        /// <param name="RetryTimeSecond">重试间隔</param>
        /// <returns></returns>
        public static void PolicyRetry(Action action, int Retry = 5, int RetryTimeSecond = 5)
        {
            //1 遇到异常 重试5次，每次间隔10s
            var policyRetry = Policy.Handle<Exception>(
                ex => ex.Message.Contains("The SSL connection")
                || ex.Message.Contains("A task was canceled")
                || ex.Message.Contains("while sending the request")
                || ex.Message.Contains("Collection was modified")
                || ex.Message.Contains("Server is busy")
                || ex.Message.Contains("The SSL connection"))
                .WaitAndRetry(
                Retry,
                retryAttempt => TimeSpan.FromSeconds(RetryTimeSecond),
                (exception, timespan, retryCount, context) =>
                {
                    // do something
                    Console.WriteLine($"重试--{retryCount},间隔秒:{ RetryTimeSecond},错误详情:{exception}");
                });

            #region 特殊规则(倍数等待) 注释
            //// 重试3次，分别等待1、2、3秒。
            //Policy.Handle<Exception>().WaitAndRetry(new[]
            //{
            //    TimeSpan.FromSeconds(1),
            //    TimeSpan.FromSeconds(2),
            //    TimeSpan.FromSeconds(3)
            //}); 
            #endregion

            //2 执行业务代码
            policyRetry.Execute(() =>
            {
                Console.WriteLine($"开始任务--{DateTime.Now}");
                action.Invoke();
                Console.WriteLine($"完成任务--{DateTime.Now}");
            });
        }



        /// <summary>
        /// 重试含带降级补偿方法设置
        /// </summary>
        /// <param name="action">执行的方法</param>
        /// <param name="fallBackAction">补偿的方法</param>
        /// <param name="Retry">重试次数</param>
        public static void PolicyRetryFallback(Action action, Action fallBackAction = null, int Retry = 2, int RetryTimeSecond = 5)
        {
            //1 遇到异常 重试5次，每次间隔10s
            var policyRetry = Policy.Handle<Exception>(
                ex => ex.Message.Contains("The SSL connection")
                || ex.Message.Contains("A task was canceled")
                || ex.Message.Contains("while sending the request")
                || ex.Message.Contains("Collection was modified")
                || ex.Message.Contains("Server is busy")
                || ex.Message.Contains("The SSL connection"))
                .WaitAndRetry(
                Retry,
                retryAttempt => TimeSpan.FromSeconds(RetryTimeSecond),
                (exception, timespan, retryCount, context) =>
                {
                    // do something
                    Console.WriteLine($"重试--{retryCount},间隔秒:{ RetryTimeSecond},错误详情:{exception}");
                });


            //2 降级操作
            Policy policyFallback = Policy.Handle<Exception>()
                                          .Fallback(() =>
                                          {
                                              Console.WriteLine("我是降级后的执行的操作");
                                              //步奏2 我是降级后的执行的操作 x
                                              //类似于 补偿方法
                                              if (fallBackAction != null)
                                                  fallBackAction.Invoke();
                                          }, ex =>
                                          {
                                              //步奏1
                                              Console.WriteLine($"业务报错信息为：{ex?.Message}");
                                          });

            var policy = policyFallback.Wrap(policyRetry);
            //2.4 执行业务代码
            policy.Execute(() =>
            {
                Console.WriteLine($"开始任务--{DateTime.Now}");
                action.Invoke();
                Console.WriteLine($"完成任务--{DateTime.Now}");
            });
        }

        /// <summary>
        /// 熔断的方法设置
        /// </summary>
        /// <param name="action">执行的方法</param>
        /// <param name="exceptionsAllowedBeforeBreaking">连续出错次数</param>
        /// <param name="Seconds">熔断多少秒</param>
        /// <remarks>下面设置的是连续出错3次之后熔断10秒，意思是：连续出错3次后，熔断10s，在这10s内，再次访问，不再执行Execute中的代码，直接报错，</remarks>
        public static void PolicyCircuitBreaker(Action action, int exceptionsAllowedBeforeBreaking, int Seconds)
        {
            Policy policy = Policy
             .Handle<Exception>()
             .CircuitBreaker(exceptionsAllowedBeforeBreaking, TimeSpan.FromSeconds(Seconds)); //连续出错3次之后熔断10秒(不会再去尝试执行业务代码）。 

            policy.Execute(() =>
            {
                Console.WriteLine("开始任务---");
                action.Invoke();
                Console.WriteLine("完成任务---");
            });
        }

    }
}
