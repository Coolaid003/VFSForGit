﻿using NUnitLite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace RGFS.Tests
{
    public class NUnitRunner
    {
        private List<string> args;
        private List<string> excludedCategories;

        public NUnitRunner(string[] args)
        {
            this.args = new List<string>(args);
            this.excludedCategories = new List<string>();
        }

        public string GetCustomArgWithParam(string arg)
        {
            string match = this.args.Where(a => a.StartsWith(arg + "=")).SingleOrDefault();
            if (match == null)
            {
                return null;
            }
            
            this.args.Remove(match);
            return match.Substring(arg.Length + 1);
        }

        public bool HasCustomArg(string arg)
        {
            // We also remove it as we're checking, because nunit wouldn't understand what it means
            return this.args.Remove(arg);
        }

        public void ExcludeCategory(string category)
        {
            this.excludedCategories.Add("cat!=" + category);
        }

        public int RunTests(int repeatCount)
        {
            if (this.excludedCategories.Count > 0)
            {
                this.args.Add("--where=" + string.Join("&&", this.excludedCategories));
            }

            int finalResult = 0;
            for (int i = 0; i < repeatCount; i++)
            {
                Console.WriteLine("Starting pass {0}", i + 1);
                DateTime now = DateTime.Now;

                finalResult = new AutoRun(Assembly.GetEntryAssembly()).Execute(this.args.ToArray());

                Console.WriteLine("Completed pass {0} in {1}", i + 1, DateTime.Now - now);
                Console.WriteLine();

                if (i < repeatCount - 1)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
            }

            return finalResult;
        }
    }
}
