using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace GeckoLib_to_EmoteCraft_Anim_Converter
{
    internal class Program
    {
        static void Convert(string path)
        {
            JObject preconverted = JObject.Parse(File.ReadAllText(path));
        }

        [STAThread] 
        static void Main(string[] args)
        {
            while (true)
            {
                Console.WriteLine("GeckoLib -> EmoteCraft Anim Converter");
                Console.WriteLine("Created by Ossyence cuz he didnt understand how to export blockbench animations like emotecraft (very scuffed)");
                Console.WriteLine("\nPlease select EVERY GeckoLib .json to convert!\n");
                
                var dialog = new OpenFileDialog();
                dialog.Filter = "JSON file|*.json";
                dialog.Title = "Select a GeckoLib json to convert";

                if ((bool)dialog.ShowDialog()) foreach (string path in dialog.FileNames) Convert(path);

                Console.WriteLine("Would you like to convert some more? (y = yes, n = NO!!!");

                switch (Console.ReadLine().ToLower())
                {
                    case "y":
                        Console.Clear();
                        break;
                    default:
                        Console.WriteLine("BYE BYE");
                        Environment.Exit(0);
                        break;
                }
            }
        }
    }
}
