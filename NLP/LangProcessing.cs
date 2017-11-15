using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using edu.stanford.nlp.ling;
using edu.stanford.nlp.pipeline;
using java.util;

namespace NLP
{
    public class LangProcessing
    {
        // Path to the folder with models extracted from `stanford-corenlp-3.4-models.jar`
        string jarRoot = @"../../stanford-corenlp-3.8.0-models\";

        AnnotationPipeline pipeline;

        public LangProcessing()
        {
            // We should change current directory, so StanfordCoreNLP could find all the model files automatically 
            var curDir = Environment.CurrentDirectory;
            Directory.SetCurrentDirectory(jarRoot);
            Properties props = new Properties();
            props.setProperty("annotators", "tokenize, ssplit, pos");
            pipeline = new StanfordCoreNLP(props);
            Directory.SetCurrentDirectory(curDir);
            
        }

        //
        public void Process(ref RecipeItem rec)
        {

            string text = rec.Value.ToLower();
            // Annotation
            var annotation = new Annotation(text);
            pipeline.annotate(annotation);

            // these are all the sentences in this document
            // a CoreMap is essentially a Map that uses class objects as keys and has values with custom types
            var sentences = annotation.get(typeof(CoreAnnotations.SentencesAnnotation));

            if (sentences == null)
            {
                return;
            }

           
            var adj = "";
            var noun = "";
            
            foreach (Annotation sentence in sentences as ArrayList)
            {
                
                //var token = sentence.get(typeof(CoreAnnotations.PartOfSpeechAnnotation));
                var token = sentence.get(typeof(CoreAnnotations.TokensAnnotation));
                CoreLabel prev = new CoreLabel();
                CoreLabel next;
                bool isNote = false;
                foreach (CoreLabel typ in token as ArrayList)
                {
                    object word = typ.get(typeof(CoreAnnotations.TextAnnotation));
                    var pos = typ.get(typeof(CoreAnnotations.PartOfSpeechAnnotation));

                    Console.WriteLine("type: {0}, word: {1}", pos, word);
                    string test = pos.ToString().ToLower();
                    if (isNote)
                        rec.Notes += " " + word;

                    if (test.Contains(","))
                    {
                        isNote = true;
                    }

                    

                    if (test.Contains("jj"))
                        adj += " " + word;

                    if (test.Contains("nn"))
                        noun += " " + word;

                    if (prev.value() != null)
                    {
                        word = prev.get(typeof(CoreAnnotations.TextAnnotation));
                        pos = prev.get(typeof(CoreAnnotations.PartOfSpeechAnnotation));

                    }

                    prev = typ;
                }
                
            }
            Console.WriteLine("\n");
            rec.Adj = adj;
            rec.Noun = noun;
        }
    }
}
