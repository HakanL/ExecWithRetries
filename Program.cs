using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
namespace ExecWithRetries
{
	internal class Program
	{
		private static void DisplayHelp()
		{
			Console.WriteLine("Executes a command after waiting a little bit, with optional retries.\r\n\r\nUsage: ExecWithRetries /c:<command> /a:<arguments> [/d:delayMs] [/r:retries] [/rd:retryDelayMs]\r\n\r\n\t/c:     Command to execute\r\n\t/a:     Arguments for the command\r\n\t/e:     Expected return codes (comma separated list).\r\n\t        If the command doesnt return the expected code, it counts as a failure\r\n\t/d:     How long to wait before first attempt in milliseconds (default: 0)\r\n\t/r:     Number of retries on failure (default: 0)\r\n\t/rd:    How long to wait between retries in milliseconds (default: 10000)\r\n");
		}
		private static int Main(string[] args)
		{
			int result;
			try
			{
				Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
				dictionary.Add("c", null);
				dictionary.Add("a", null);
				dictionary.Add("e", null);
				dictionary.Add("d", null);
				dictionary.Add("r", null);
				dictionary.Add("rd", null);
				int num = Program.LoadArguments(dictionary, args);
				if (num <= 0)
				{
					Program.DisplayHelp();
					result = 2;
				}
				else
				{
					string text = Path.Combine(Directory.GetCurrentDirectory(), dictionary["c"]);
					string text2 = null;
					dictionary.TryGetValue("a", out text2);
					int num2 = 0;
					string text3;
					if ((text3 = dictionary["d"]) != null)
					{
						num2 = int.Parse(text3);
					}
					int num3 = 0;
					if ((text3 = dictionary["r"]) != null)
					{
						num3 = int.Parse(text3);
					}
					int num4 = 10000;
					if ((text3 = dictionary["rd"]) != null)
					{
						num4 = int.Parse(text3);
					}
					int[] array = new int[1];
					int[] expectedReturnCodes = array;
					if ((text3 = dictionary["e"]) != null)
					{
						expectedReturnCodes = (
							from s in text3.Split(new char[]
							{
								','
							}, StringSplitOptions.RemoveEmptyEntries)
							select int.Parse(s)).ToArray<int>();
					}
					Console.WriteLine("Executing '{0}{1}' with initial delay of {2}ms and up to {3} retries with {4}ms delay.", new object[]
					{
						text,
						(text2 != null) ? (" " + text2) : string.Empty,
						num2,
						num3,
						num4
					});
					int num5 = 1 + num3;
					int millisecondsTimeout = num2;
					for (int i = 0; i < num5; i++)
					{
						Thread.Sleep(millisecondsTimeout);
						try
						{
							Program.ExecuteProcess(text, text2, expectedReturnCodes);
							Console.Error.WriteLine("Attempt #{0} succeeded", i + 1);
							result = 0;
							return result;
						}
						catch (Exception ex)
						{
							Console.Error.WriteLine("Attempt #{0} failed: {1}", i + 1, ex.Message);
						}
						millisecondsTimeout = num4;
					}
					Console.Error.WriteLine("Failed");
					result = 1;
				}
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine("Error: {0}", ex);
				result = 1;
			}
			return result;
		}
		private static int LoadArguments(Dictionary<string, string> supportedArguments, string[] argsToParse)
		{
			int num = 0;
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			for (int i = 0; i < argsToParse.Length; i++)
			{
				string text = argsToParse[i];
				if (!text.StartsWith("/"))
				{
					throw new ArgumentException("Argument didnt follow '/arg:value' format: " + text);
				}
				int num2 = text.IndexOf(':');
				if (num2 < 0)
				{
					throw new ArgumentException("Argument didnt follow '/arg:value' format: " + text);
				}
				string key = text.Substring(1, num2 - 1);
				string value = text.Substring(num2 + 1);
				if (!supportedArguments.ContainsKey(key))
				{
					throw new ArgumentException("Unrecognized argument: " + text);
				}
				supportedArguments[key] = value;
				num++;
			}
			return num;
		}
		private static void ExecuteProcess(string name, string args)
		{
			int[] expectedReturnCodes = new int[1];
			Program.ExecuteProcess(name, args, expectedReturnCodes);
		}
		private static void ExecuteProcess(string name, string args, int[] expectedReturnCodes)
		{
			try
			{
				Process process = new Process();
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.Arguments = args;
				process.StartInfo.FileName = name;
				process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.RedirectStandardError = true;
				process.StartInfo.RedirectStandardOutput = true;
				int maxOutputSize = 16384;
				StringBuilder output = new StringBuilder(maxOutputSize);
				process.OutputDataReceived += delegate(object o, DataReceivedEventArgs e)
				{
					if (e.Data != null && output.Length + e.Data.Length <= maxOutputSize)
					{
						output.AppendLine(e.Data);
					}
				};
				process.ErrorDataReceived += delegate(object o, DataReceivedEventArgs e)
				{
					if (e.Data != null && output.Length + e.Data.Length <= maxOutputSize)
					{
						output.AppendLine(e.Data);
					}
				};
				process.Start();
				process.BeginErrorReadLine();
				process.BeginOutputReadLine();
				process.WaitForExit();
				if (!expectedReturnCodes.Contains(process.ExitCode))
				{
					throw new Exception(string.Format("Process exited with {0}:\r\n{1}", process.ExitCode, output.ToString()));
				}
			}
			catch (Exception ex)
			{
				throw new Exception(string.Format("Command '{0} {1}' failed.\r\n{2}", name, args, ex.Message));
			}
		}
	}
}
