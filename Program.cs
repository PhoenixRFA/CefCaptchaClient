using CefCaptchaResolver;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace CefCaptchaClient
{
    internal class Program
    {
        private static string _localPath;

        static void Main(string[] args)
        {
            _localPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase).Substring("file:/".Length);
            var resolver = new Resolver();

            Task.Run(async () =>
            {
                await resolver.Init(_localPath);

                //await resolver.ProvokeCaptcha();
                await _retrieveApiKey(resolver);

                Console.ReadLine();
            }).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private static async Task _retrieveApiKey(Resolver resolver)
        {
            string res = await resolver.RetrieveYandexTranslateApiKey();

            if (res == "no sid" || res == string.Empty)
            {
                //captcha
                await _resolveCaptcha(resolver);
                await _retrieveApiKey(resolver);
            }
            else
            {
                Console.Write(res);
            }
        }

        private static async Task _resolveCaptcha(Resolver resolver)
        {
            while (true)
            {
                MemoryStream stream = await resolver.RetrieveCaptchaImage();

                using (FileStream fileStream = File.Create("captcha.png"))
                {
                    stream.CopyTo(fileStream);
                }

                string captchaPath = Path.Combine(_localPath, "captcha.png");
                Process process = Process.Start(new ProcessStartInfo(captchaPath)
                {
                    UseShellExecute = true
                });

                string userInput;
                do
                {
                    Console.Write("read and type text: ");
                    userInput = Console.ReadLine();
                }
                while (userInput.Length < 1);

                bool isOk = await resolver.ResolveCaptcha(userInput);

                if (isOk)
                {
                    _log("captcha successfully resolved!");
                    break;
                }

                _log("captcha failed, try again");
            }
        }

        private static void _log(string msg)
        {
            var dt = DateTime.Now;
            Console.WriteLine($"{dt:dd.MM.yyyy_HH:mm:ss}: {msg}");
        }
    }
}
