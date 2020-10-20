﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using PuppeteerExtraSharp.Plugins.Recaptcha.Provider;
using PuppeteerExtraSharp.Utils;
using PuppeteerSharp;

namespace PuppeteerExtraSharp.Plugins.Recaptcha
{
    public class Recaptcha
    {
        private readonly IRecaptchaProvider _provider;
        private readonly CaptchaOptions _options;

        public Recaptcha(IRecaptchaProvider provider, CaptchaOptions options)
        {
            _provider = provider;
            _options = options;
        }

        public async Task<RecaptchaResult> Solve(Page page)
        {
            try
            {
                var key = await GetKeyAsync(page);
                var solution = await GetSolutionAsync(key, page.Url);
                await WriteToInput(page, solution);

                return new RecaptchaResult()
                {
                    IsSuccess = true
                };
            }
            catch (CaptchaException ex)
            {
                return new RecaptchaResult()
                {
                    Exception = ex,
                    IsSuccess = false
                };
            }

        }

        public async Task<string> GetKeyAsync(Page page)
        {
            var element =
                await page.QuerySelectorAsync("iframe[src^='https://www.google.com/recaptcha/api2/anchor'][name^=\"a-\"]");

            if (element == null)
                throw new CaptchaException(page.Url, "Recaptcha key not found!");

            var src = await element.GetPropertyAsync("src");

            if (src == null)
                throw new CaptchaException(page.Url, "Recaptcha key not found!");

            var key = HttpUtility.ParseQueryString(src.ToString()).Get("k");
            return key;
        }

        public async Task<string> GetSolutionAsync(string key, string urlPage)
        {
            return await _provider.GetSolution(key, urlPage);
        }

        public async Task WriteToInput(Page page, string value)
        {
            await page.EvaluateFunctionAsync(
                  $"() => {{document.getElementById('g-recaptcha-response').innerHTML='{value}'}}");


            var script = ResourcesReader.ReadFile(this.GetType().Namespace + ".Scripts.FindRecaptcha.js");

            try
            {
                await page.EvaluateFunctionAsync($"() => ({script})().function('{value}')");
            }
            catch
            {
                // ignored
            }
        }
    }
}
