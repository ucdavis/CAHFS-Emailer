

using Hangfire.Storage.Monitoring;
using NLog;

namespace CAHFS_Emailer.Services
{
 
    public class EmailSender
    {
        //semaphore to ensure multiple jobs don't run at the same time
        private static readonly SemaphoreSlim semaphore = new(1, 1);
        public enum EmailerStatus { Available, Checking, Sending, Finishing }
        private static EmailerStatus emailerStatus = EmailerStatus.Available;
        
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        

        public static async Task EmailSendkJob()
        {
            logger.Info($"EmailSendkJob started at: {DateTime.UtcNow:HH:mm:ss}");

            if(semaphore.Wait(0))
            {
                emailerStatus = EmailerStatus.Checking;
                await SendEmail();
            }
            else
            {
                logger.Warn($"Exiting EmailSendkJob - Status is {emailerStatus}");
            }

            if (emailerStatus != EmailerStatus.Available)
            {
                
            }
            
            Console.WriteLine($"Scheduled at: {DateTime.UtcNow:HH:mm:ss}");
        }

        private static async Task SendEmail()
        {
            //get emails from the database
            

        }
    }
}
