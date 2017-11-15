using System;
using System.Collections.Generic;
using HtmlAgilityPack;
using Google.Apis.Customsearch.v1.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using edu.stanford.nlp.ling;
using edu.stanford.nlp.pipeline;
using java.util;

namespace NLP
{
    class Program
    {

        class LogStuff
        {
            
            public int recipeTotal { get; set; }
            public int yieldTotal { get; set; }
            public int maxYield { get; set; }
            public int minYield { get; set; }

            public LogStuff()
            {
                recipeTotal = 0;
                yieldTotal = 0;
                maxYield = 0;
                minYield = 100;
            }
        }

        static void Main(string[] args)
        {
            var compositeRecipe = new Dictionary<string, List<RecipeItem>>();
            var langProc = new LangProcessing();

            const string str = "I went or a run. Then I went to work. I had a good lunch meeting with a friend name John Jr. The commute home was pretty good.";
            var rec = new RecipeItem();
            rec.Value = str;
            //langProc.Process(ref rec);

            //handl
            string query;
            if (args.Length > 0)
                query = args[0] ?? "french onion soup";
            else
                query = "Fried okra";

            var ih = new InternetHelpers();
            var rf = new RecipeFactory();
            var r = new Recipe();

            IList<Result> paging = new List<Result>();
            IList<Recipe> recipes = new List<Recipe>();

            var listRequest = ih.GetUrls(query);


            var count = 0;

            var logStuff = new LogStuff();

            logStuff.recipeTotal = 0;
            logStuff.yieldTotal = 0;

            var yeildArr = new List<int>();

            while (paging != null && count < 1)
            {

                listRequest.Start = count * 10 + 1;
                paging = listRequest.Execute().Items;
                

                foreach (var item in paging)
                {
                    
                    HtmlNodeCollection nodes = ih.GetNodes(item.Link);
                    r = new Recipe();

                    if (nodes != null && item.Link.Contains("recipe"))
                    {
                        Console.WriteLine("\n{0}\n", item.Link);

                        var node = nodes[0];

                        JObject jRecipe = JObject.Parse(node.InnerHtml);
                        IList<JsonRecipe> searchResults = new List<JsonRecipe>();
                        

                        r.recipeUrl = item.Link;
                        if (jRecipe.TryGetValue("name", out JToken val))
                        {
                            r.name = val.ToString();
                        }

                        if (jRecipe.TryGetValue("recipeYield", out val))
                        {
                            var temp = val.ToString();
                            var yield = rf.ParseYield(temp);

                            if (yield > logStuff.maxYield)
                                logStuff.maxYield = yield;

                            if (yield < logStuff.minYield)
                                logStuff.minYield = yield;

                            logStuff.recipeTotal++;
                            logStuff.yieldTotal += yield;
                            r.recipeYield = yield;

                        }

                        if (jRecipe.TryGetValue("cookTime", out val))
                        {
                            r.cookTime = val.ToString();
                        }

                        if (jRecipe.TryGetValue("prepTime", out val))
                        {
                            r.prepTime = val.ToString();
                        }

                        if (jRecipe.TryGetValue("totalTime", out val))
                        {
                            r.totalTime = val.ToString();
                        }

                        if (jRecipe.TryGetValue("recipeInstructions", out val))
                        {
                            var count2 = 1;
                            foreach (JToken result in val.Children().Values())
                            {
                                // JToken.ToObject is a helper method that uses JsonSerializer internally
                                var searchResult = result.ToString();
                                r.instructions.Add(count2, searchResult);
                                count2++;
                            }
                        }

                        if (jRecipe.TryGetValue("recipeIngredient", out val))
                        {
                            foreach (JToken result in val.Children().Values())
                            {
                                // JToken.ToObject is a helper method that uses JsonSerializer internally
                                var searchResult = result.ToString();
                                var temp = rf.ParseRecipeItem(searchResult);

                                langProc.Process(ref temp);

                                var noun = temp.Noun;

                                if (!r.ingredients.ContainsKey(noun))
                                {
                                    r.ingredients.Add(noun, temp);
                                }
                                

                                if (compositeRecipe.ContainsKey(noun))
                                {
                                    compositeRecipe[noun].Add(temp);
                                }
                                else
                                {
                                    List<RecipeItem> tList = new List<RecipeItem>();
                                    tList.Add(temp);
                                    compositeRecipe.Add(noun, tList);
                                }
                                
                            }
                        }

                        recipes.Add(r);
                    }

                }
                count++;
            }

            foreach (var ri in compositeRecipe)
            {
                Console.WriteLine("item: {0}, count: {1}\n", ri.Key, ri.Value.Count);
                foreach (var item in ri.Value)
                {
                    Console.WriteLine("\tname : {0}", item.Adj.Trim() + " " + item.Noun.Trim());
                }
            }

            var avgYield = logStuff.yieldTotal / logStuff.recipeTotal;

            foreach (var ri in recipes)
            {
                var recipeRatio = (double) ri.recipeYield / avgYield;

                foreach (var i in ri.ingredients)
                {
                    i.Value.Amount = i.Value.Amount * recipeRatio;
                }

               // PrintRecipe(ri);
            }


            Console.WriteLine("\nratio: {0} minYield: {1} maxYield: {2}\n", avgYield, logStuff.minYield, logStuff.maxYield);
            return;
        }

        //
        public static void PrintRecipe(Recipe ri)
        {
            Console.WriteLine("\nname:  {0}", ri.name);
            Console.WriteLine("\nLink: {0}", ri.recipeUrl);
            Console.WriteLine("\nyield: {0}", ri.recipeYield);
            Console.WriteLine("\ncook time:  {0}", ri.cookTime);
            Console.WriteLine("prep time:  {0}", ri.prepTime);
            Console.WriteLine("total time: {0}", ri.totalTime);
            Console.WriteLine("\nIngredients:");
            foreach (var i in ri.ingredients)
            {
                string temp = "";
                if (Math.Abs(i.Value.Amount) > 0.0)
                {
                    temp += i.Value.Amount.ToString();
                }
                if (Math.Abs(i.Value.UpAmount) > 0.0)
                {
                    temp += " to " + i.Value.UpAmount.ToString();
                }

                Console.WriteLine("\t{0} unit:{1} rest:{2}", temp, i.Value.MessumentUnit, i.Value.Value);
            }

            Console.WriteLine("\nInstructions:");
            foreach (var i in ri.instructions)
            {
                Console.WriteLine("\t{0} {1}", i.Key, i.Value);
            }
        }
    }
}
