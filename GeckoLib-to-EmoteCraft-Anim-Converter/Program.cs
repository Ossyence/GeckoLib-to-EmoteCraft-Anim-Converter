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
        class Vector3
        {
            public double x = 0;
            public double y = 0;
            public double z = 0;

            public Vector3(double Sx = 0, double Sy = 0, double Sz = 0)
            {
                x = Sx;
                y = Sy;
                z = Sz;
            }

            public Vector3 Inverse() { x *= -1; y *= -1; z *= -1; return this; }

            public Vector3 Add(Vector3 vec) { x += vec.x; y += vec.y; z += vec.z; return this; }

            public Vector3 Subtract(Vector3 vec) { x -= vec.x; y -= vec.y; z -= vec.z; return this; }

            public Vector3 Multiply(Vector3 vec) { x *= vec.x; y *= vec.y; z *= vec.z; return this; }
            public Vector3 Multiply(double factor) { x *= factor; y *= factor; z *= factor; return this; }

            public Vector3 Divide(Vector3 vec) { x /= vec.x; y /= vec.y; z /= vec.z; return this; }
            public Vector3 Divide(double factor) { x /= factor; y /= factor; z /= factor; return this; }
        }

        static Dictionary<string, Vector3[]> offsets = new Dictionary<string, Vector3[]>()
        {
            { "body", new Vector3[] { new Vector3(), new Vector3() } },

            { "leftLeg",  new Vector3[] { new Vector3(-5, 12), new Vector3() } },
            { "left_leg", new Vector3[] { new Vector3(-5, 12), new Vector3() } },

            { "rightLeg",  new Vector3[] { new Vector3(5, 12), new Vector3() } },
            { "right_leg", new Vector3[] { new Vector3(5, 12), new Vector3() } },

            { "leftArm",  new Vector3[] { new Vector3(-5), new Vector3() } },
            { "left_arm", new Vector3[] { new Vector3(-5), new Vector3() } },

            { "rightArm",  new Vector3[] { new Vector3(5), new Vector3() }  },
            { "right_arm", new Vector3[] { new Vector3(5), new Vector3() }  },

            { "rightItem",  new Vector3[] { new Vector3(), new Vector3() }  },
            { "right_item", new Vector3[] { new Vector3(), new Vector3() }  },

            { "leftItem",  new Vector3[] { new Vector3(), new Vector3() }  },
            { "left_item", new Vector3[] { new Vector3(), new Vector3() }  },

            { "torso", new Vector3[] { new Vector3(0, 12), new Vector3() } },

            { "head", new Vector3[] { new Vector3(), new Vector3() } },
        };

        static int TimeToTick(double time)
        {
            return (int)Math.Floor(time * 24);
        }

        static double ToDouble(string s)
        {
            return double.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
        }

        static void ConvertAnim(string path)
        {
            string name = Path.GetFileName(path).Replace(".json", "");

            Console.WriteLine($"\nConverting - \n\"{name}\"\n\"{path}\"\n");

            string author = AskNonrestrictive($"Whos the author?");
            string description = AskNonrestrictive($"Describe this animation");
            int easeIn = TimeToTick(ToDouble(AskNonrestrictive($"How much time to ease into the anim?")));
            int easeOut = TimeToTick(ToDouble(AskNonrestrictive($"How much time to ease out of the anim?")));
            bool applyOffsetsPref = Ask($"Do you want to automatically apply the player offsets (ONLY WORKS FOR PLAYERS)");
            bool looped = Ask($"Looped?");

            Console.WriteLine($"\nBegining conversion...\n\nParsing JSON...");

            JObject preconverted = JObject.Parse(File.ReadAllText(path));
            JObject bones = (JObject)preconverted["animations"][name]["bones"];

            double value = (double)preconverted["animations"][name]["animation_length"];

            var length = TimeToTick(value);

            JArray moveArray = new JArray();

            Console.WriteLine($"Looking into moves and formatting...");

            List<JObject> moves = new List<JObject>();

            foreach (JProperty bodyPart in (JToken)bones) {
                string bodyPartName = bodyPart.Name;
                JObject children = (JObject)bodyPart.Value;

                JObject rot = (JObject)children["rotation"];
                JObject pos = (JObject)children["position"];

                void AssistMe(JObject lookup, string xName, string yName, string zName, bool isPosition) {
                    foreach (JProperty values in (JToken)lookup)
                    {
                        JObject done = new JObject();

                        double time = 0;
                        bool specialCase = false;

                        try { time = ToDouble(values.Name); } catch { specialCase = true; }

                        int tick = TimeToTick(time);

                        JObject container;

                        JArray vector;
                        string easing;

                        if (!specialCase)
                        {
                            container = (JObject)values.Value;

                            vector = (JArray)container["vector"];
                            easing = (string)container["easing"];
                        } else
                        {
                            vector = (JArray)values.Value;
                            easing = null;
                        }

                        Vector3 xyz = new Vector3((double)vector[0], (double)vector[1], (double)vector[2]);

                        if (applyOffsetsPref) {
                            if (offsets.ContainsKey(bodyPartName))
                            {
                                Vector3[] partOffsets = offsets[bodyPartName];
                                Vector3 offset = new Vector3();

                                if (isPosition)
                                    offset = partOffsets[0];
                                else
                                    offset = partOffsets[1];

                                xyz.Add(offset);
                            }
                        }

                        done.Add("tick", tick);

                        if (easing != null) { done.Add("easing", easing.ToUpper()); }

                        done.Add("turn", 0);
                        done.Add(bodyPartName, new JObject
                        {
                            { xName, xyz.x },
                            { yName, xyz.y },
                            { zName, xyz.z }
                        });

                        moves.Add(done);
                    }
                }

                if (rot != null) AssistMe(rot, "pitch", "yaw", "roll", false);
                if (pos != null) AssistMe(pos, "x", "y", "z", true);
            }

            Console.WriteLine($"Sorting moves by tick...");

            int tickSort(JObject now, JObject next)
            {
                int nowTick = (int)now["tick"];
                int nextTick = (int)next["tick"];

                if (nowTick > nextTick) {
                    return 1;
                } else if (nowTick == nextTick) {
                    return 0;
                } else if (nowTick < nextTick) {
                    return -1;
                }

                return 0;
            }

            moves.Sort(tickSort);

            Console.WriteLine($"Adding to \"moves\" JSON array...");
            
            foreach (JObject data in moves) moveArray.Add(data);

            Console.WriteLine($"Creating base EmoteLib JSON...");

            JObject emoteData = new JObject
            {
                { "isLoop", BoolString(looped) },
                { "beginTick", easeIn },
                { "stopTick", easeOut },
                { "returnTick", 0 },
                { "endTick", length },
                { "degrees", true },
                { "moves", moveArray },
            };

            JObject array = new JObject
            {
                { "name", name },
                { "author", author },
                { "description", description },
                { "emote", emoteData }
            };

            Console.WriteLine($"Writing to file...");

            string newName = name + "_degeckoed";

            string newPath = path.Replace(name, newName);

            if (File.Exists(newPath)) {
                Console.WriteLine($"Overwriting previous conversion..."); 
                File.Delete(newPath); 
            }

            File.WriteAllText(newPath, array.ToString());

            Console.WriteLine($"Fully converted \"{name}\" saved as \"{newName}\"!\n");
        }

        static string BoolString(bool b)
        {
            switch (b)
            {
                case true:
                    return "true";
                default:
                    return "false";
            }
        } 

        static string AskNonrestrictive(string question)
        {
            Console.WriteLine(question);

            return Console.ReadLine();
        }

        static bool Ask(string question)
        {
            switch (AskNonrestrictive($"{question} (y = yes, n = no)").ToLower())
            {
                case "y":
                    return true;
                default:
                    return false;
            }
        }

        [STAThread] 
        static void Main(string[] args)
        {
            while (true)
            {
                Console.WriteLine("GeckoLib -> EmoteCraft Anim Converter");
                Console.WriteLine("Created by Ossyence cuz he didnt understand how to export blockbench animations like emotecraft (very scuffed)");
                Console.WriteLine("THIS WILL STRIP THE \"easingArgs\" FROM THE ANIMATION!!!");
                
                Console.WriteLine("\nPlease select EVERY GeckoLib .json to convert!\n");
                
                var dialog = new OpenFileDialog();
                dialog.Filter = "JSON file|*.json";
                dialog.Title = "Select a GeckoLib json to convert";
                dialog.Multiselect = true;

                if ((bool)dialog.ShowDialog()) foreach (string path in dialog.FileNames) ConvertAnim(path);

                switch (Ask("Convert some more animations?"))
                {
                    case true:
                        Console.Clear();
                        break;
                    case false:
                        Console.WriteLine("BYE BYE!!!!$#@!#^%!@\n");
                        Environment.Exit(0);
                        break;
                }
            }
        }
    }
}
