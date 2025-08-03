/*
services.AddChronux(cfg =>
   {
       cfg.UseInMemoryStorage();
   
       cfg.AddJob<MyJob>("SendEmail")
          .WithCron("0 0 * * *") // every day
          .WithRetryPolicy(maxAttempts: 3)
          .OnSuccess(next: "LogResultJob");
   
       cfg.AddJob<OtherJob>("LogResultJob")
          .WithDelay(TimeSpan.FromMinutes(5));
   });
   */
   
   /*
    // for Console app to keep alive
    await ChronuxHost.RunAsync(cfg =>
      {
          cfg.UseInMemoryStorage();
          cfg.AddJob<SyncJob>("hello").WithInterval(TimeSpan.FromSeconds(10));
      });
      
      services.AddChronux(cfg =>
      {
          cfg.UseMiddleware<LoggingMiddleware>();
      
          cfg.AddJob<SendEmailHandler, SendEmailContext>("SendEmail")
             .WithTrigger(new CronTrigger(...))
             .Register();
      });
    */
    
    /*
     // Worker
       IHostedService
     */
     
     /*
     🧠 Behavior Details
        Host Type	Chronux Behavior
        Worker	You control the host loop, and Chronux registers as a background service (via IHostedService)
        Console App	Chronux can keep the process alive (via internal background runner) if you opt in 
        Web App Jobs are scheduled in the background, same as Hangfire/Quartz does
    */
    
    
    
     /*public sealed class SendEmailHandler(IEmailService emailService) : IJobHandler<SendEmailContext>
     {
         public async ValueTask<JobResult> ExecuteAsync(JobContext<SendEmailContext> context, CancellationToken ct)
         {
             await emailService.SendAsync(context.Input.To, context.Input.Body, ct);
             return JobResult.Successed();
         }
     }*/
     
     /*
     builder.AddJob<SendEmailHandler, SendEmailContext>("SendEmail")
        .WithTrigger(new CronTrigger("daily", "0 8 * * *"))
        .WithRetryPolicy(new RetryPolicy { MaxAttempts = 3, Delay = TimeSpan.FromMinutes(5) })
        .WithChainedJobs(onSuccess: ["LogSuccess"])
        .WithDescription("Send daily email to customers")
        .Register();
        
        */
        
        /**
services.AddSingleton<IJobRegistry, JobRegistry>();
services.AddSingleton<IJobExecutor, JobExecutor>();
services.AddSingleton<IJobDispatcher, JobDispatcher>();
services.AddSingleton<IDistributedLockProvider, InMemoryLockProvider>();
services.AddSingleton<ITriggerScheduler, TriggerScheduler>();

*/

/*
services.AddChronux(chronux =>
   {
       chronux.UseMiddleware<LoggingMiddleware>();
   
       chronux.AddJob<SendEmailHandler, SendEmailContext>("SendEmail")
              .WithTrigger(new CronTrigger("daily", "0 8 * * *"))
              .WithRetryPolicy(new RetryPolicy { MaxAttempts = 3, Delay = TimeSpan.FromSeconds(10) })
              .WithDescription("Sends a daily email")
              .Register();
   });
   */
   
   // Missfire
   /*
   🧱 Implementation Plan (For Now)
      Since we don’t have a durable store or persisted last-run times yet, we will:
      
      ✅ Add support for future hooks and settings
      🚫 Do not yet implement full detection logic
      
      ✅ Step 8 Scope:
      Feature	Action
      EnableMisfireHandling (global + per-job)	✅ respected
      TriggerScheduler aware of flag	✅ placeholder logic
      Mark trigger as “misfired” if time missed	✅ with warning log
      No durable re-run logic yet	❌ postponed to persistence phase
      */
      
      /**
      var enqueuer = sp.GetRequiredService<IJobEnqueuer>();

await enqueuer.EnqueueAsync("recalculate-metrics", new MetricsInput { Force = true });
*/

/*
services.AddChronux(builder => ..., options =>
   {
       options.Retention = new ChronuxRetentionOptions
       {
           MaxAge = TimeSpan.FromDays(7),
           MaxCountPerJob = 1000
       };
   });
   */
   
   /*
   SQL server
   builder.Services.AddChronux(cfg =>
      {
          cfg.AddJob<...>(...).WithTrigger(...);
      }, options =>
      {
          options.SerializerInstance = new JsonChronuxSerializer();
          options.UseSqlServerStorage(builder.Services, "your-connection-string");
      });
      */