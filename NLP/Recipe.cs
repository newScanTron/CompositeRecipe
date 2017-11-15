using System;
using HtmlAgilityPack;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.IO;

namespace NLP
{

    public class Recipe
	{
        public string name { get; set; }
        public int recipeYield { get; set; }
        public string cookTime { get; set; }
        public string prepTime { get; set; }
        public string totalTime { get; set; }
        public string recipeUrl { get; set; }
        public Dictionary<string, RecipeItem> ingredients { get; set; }
        public Dictionary<int, string> instructions { get; set; }

		public Recipe()
		{
			ingredients = new Dictionary<string, RecipeItem>();
            instructions = new Dictionary<int, string>();
		}
	}

    public class JsonRecipe
    {
        public string Name { get; set; }
        public string[] RecipeIngredients { get; set; }
        public string[] RecipeInstructions { get; set; }
    }

	public class RecipeItem
	{
		public double Amount { get; set; }
		public double UpAmount { get; set; }
        public string MessumentUnit { get; set; }
		public string Value { get; set; }
        public string Adj { get; set; }
        public string Noun { get; set; }
        public string Notes { get; set; }

        //Zero out everyting in the initializer.
        public RecipeItem(){
            Amount = 0;
            UpAmount = 0;
            MessumentUnit = "";
            Value = "";
            Notes = "";
        }
	}

    public class RecipeStep
    {
        public double Duration { get; set; }
        public string Value { get; set; }
    }

    public class AllUnits
    {
        public List<Units> unitTypes;

        public AllUnits() {
            unitTypes = new List<Units>();
        }
    }

    //class to hold a list so we can read in some Json data
    public class Units
    {
      public List<Unit> units;
        public string name;

      public Units() {
         units = new List<Unit>();

      }

    }
    //This class represents messument units and is used to deserialize Json data
    public class Unit
    {
        public string name { get; set; }
        public string regex { get; set; }
    }

	public class RecipeFactory
	{
		const int EPSILON = 0;
        const string wRegEx = @"(?<words>[(\w\W\s)]+)";
        //regex pattern to find parantheses holding most any ah
        const string parenRegEx = @"(?<alt>[(][\w\W\s]+[)])?";

        //ParseUnit takes in a reffrenct to a recipe item, sets the messu
        //unit and value of recipe item.
        string ParseUnit(ref RecipeItem str)
        {

            Console.WriteLine("----------------------------------");
            Console.WriteLine("str: {0}", str.Value);
            var ur = new Units();
            var unit = JsonConvert.DeserializeObject<AllUnits>(File.ReadAllText(@"../../Json/Units.json"));
            // var cupReg = unit.unitRegex[0];
            var loopRegEx = "";
            foreach (var u in unit.unitTypes) {
                foreach (var l in u.units)
                {

                    loopRegEx = l.regex + wRegEx;
                    //Console.WriteLine("regeex: {0}", loopRegEx);
                    var match = new Regex(loopRegEx).Match(str.Value.ToLower());
                    if (match.Success)
                    {
                        str.MessumentUnit = l.name;
                        str.Value = match.Groups["words"].Value;




                        Console.WriteLine("we made a {0} match: {1}\n", str.MessumentUnit, str.Value);
                        return str.MessumentUnit;
                    }
                }
            }
            Console.WriteLine("we made no match.");
            return "";

        }

		RecipeItem ParseEitherTo(string str)
		{
			var ri = new RecipeItem();
			//this regex will seperate mixed fractions seperated by the word to with group names for each part of each
			//fraction.
            const string eitherToEither = @"((?<int1>[0-9]*)\s*)((?<num1>[0-9]+)/(?<don1>[0-9]+))*\s+to\s+((?<int2>[0-9]*)\s*)((?<num2>[0-9]+)/(?<don2>[0-9]+))*\s(?<words>[\w*\s,""]+)";
			var eitherToEitherRegEx = new Regex(eitherToEither);
			var eitherToMatches = eitherToEitherRegEx.Match(str);
			if (eitherToMatches.Success)
			{

                double tot1, tot2;

				double.TryParse(eitherToMatches.Groups["int1"].Value, out double int1);
				double.TryParse(eitherToMatches.Groups["num1"].Value, out double num1);
				double.TryParse(eitherToMatches.Groups["don1"].Value, out double don1);
				double.TryParse(eitherToMatches.Groups["int2"].Value, out double int2);
				double.TryParse(eitherToMatches.Groups["num2"].Value, out double num2);
				double.TryParse(eitherToMatches.Groups["don2"].Value, out double don2);

				//Do some math if we need to do some division.
				if (Math.Abs(don1) > EPSILON)
					tot1 = int1 + num1 / don1;
				else
					tot1 = int1;
				if (Math.Abs(don2) > EPSILON)
					tot2 = int2 + num2 / don2;
				else
					tot2 = int2;

				ri.Amount = tot1;
				ri.UpAmount = tot2;
				ri.Value = eitherToMatches.Groups["words"].Value;
			}
			return ri;
		}


		RecipeItem ParseSingleMixedFraction(string str)
		{
			var ri = new RecipeItem();
			const string intDblFind = @"(?<int1>[0-9]*)\s+(?<num1>[0-9]+)/(?<don1>[0-9]+)\s(?<words>[\w*\W*]+)";
			var intDblRegEx = new Regex(intDblFind);
			var intDblMatches = intDblRegEx.Match(str);
			if (intDblMatches.Success)
			{
				double int1, num1, don1, tot1;
				double.TryParse(intDblMatches.Groups["int1"].Value, out int1);
				double.TryParse(intDblMatches.Groups["num1"].Value, out num1);
				double.TryParse(intDblMatches.Groups["don1"].Value, out don1);
				if (Math.Abs(don1) > EPSILON)
					tot1 = int1 + num1 / don1;
				else
					tot1 = int1;

				ri.Amount = tot1;
				ri.UpAmount = 0;
				ri.Value = intDblMatches.Groups["words"].Value;
			}
			return ri;
		}

		RecipeItem ParseDouble(string str)
		{
			var ri = new RecipeItem();
			const string dblFind = @"(?<num1>[0-9]+)/(?<don1>[0-9]+)\s+(?<words>[\w*\W*]+)";
			var dblRegEx = new Regex(dblFind);
			var matches = dblRegEx.Match(str);
			if (matches.Success)
			{
				double.TryParse(matches.Groups["num1"].Value, out double num1);
				double.TryParse(matches.Groups["don1"].Value, out double don1);
				var tot = num1 / don1;
				ri.Amount = tot;
				ri.UpAmount = 0;
				ri.Value = matches.Groups["words"].Value;
			}

			return ri;
		}



        RecipeItem ParseInt(string str)
        {
            var ri = new RecipeItem();
            const string intFind = @"(?<int1>[0-9]+)\s+(?<words>[\w*\W*]+)";
            var intRegEx = new Regex(intFind);
            var matches = intRegEx.Match(str);
			if (matches.Success)
			{
                double.TryParse(matches.Groups["int1"].Value, out double int1);
                ri.Amount = int1;
                ri.UpAmount = 0;
                ri.Value = matches.Groups["words"].Value;
			}

            return ri;
        }

        public int ParseYield(string str)
        {
            var i = 0;
            const string re = @"((?<int1>[0-9]*)\s*)([to\s]*)((?<int2>[0-9]*)\s*)\s(?<words>[\w*\s,""]+)";
            var intRegEx = new Regex(re);
            var matches = intRegEx.Match(str);
            int num1, num2;
            if (matches.Success)
            {
                int.TryParse(matches.Groups["int1"].Value, out num1);
                int.TryParse(matches.Groups["int2"].Value, out num2);

                if (num1 != 0 && num2 != 0)
                {
                    return (num1 + num2) / 2;
                }
                if (num1 != 0)
                {
                    return num1;
                }
            }
 
            return i;
        }

		//Function to parse recipe value
		public RecipeItem ParseRecipeItem(string str)
		{
			var ri = new RecipeItem();

			var recipeItem = ParseEitherTo(str);
			if ((recipeItem != null) && (Math.Abs(recipeItem.Amount) > EPSILON))
			{
                recipeItem.MessumentUnit = ParseUnit(ref recipeItem);
                return recipeItem;
			}

			recipeItem = ParseSingleMixedFraction(str);
			if (Math.Abs(recipeItem.Amount) > EPSILON)
			{
				recipeItem.MessumentUnit = ParseUnit(ref recipeItem);
                return recipeItem;
			}

			recipeItem = ParseDouble(str);
			if (Math.Abs(recipeItem.Amount) > EPSILON)
			{
				recipeItem.MessumentUnit = ParseUnit(ref recipeItem);
                return recipeItem;
			}

			recipeItem = ParseInt(str);
			if (Math.Abs(recipeItem.Amount) > EPSILON)
			{
				recipeItem.MessumentUnit = ParseUnit(ref recipeItem);
                return recipeItem;
			}

			ri.Value = str;
			ri.Amount = 0;
			ri.UpAmount = 0;
			return ri;
		}
	}
}
