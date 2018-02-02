//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
//
// Microsoft Cognitive Services (formerly Project Oxford): https://www.microsoft.com/cognitive-services

//
// Microsoft Cognitive Services (formerly Project Oxford) GitHub:
// https://github.com/Microsoft/ProjectOxford-ClientSDK

//
// Copyright (c) Microsoft Corporation
// All rights reserved.
//
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//



using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http;

namespace Microsoft.Cognitive.LUIS
{

    /// <summary>
    /// Reresents the results of a natural langage string parse by the LUIS service.
    /// </summary>
    public class LuisResult
    {
        public Func<string, LuisResult> Reply { get; private set; }

        /// <summary>
        /// Original text sent to the LUIS service for parsing.
        /// </summary>
        public string OriginalQuery { get; set; }

        /// <summary>
        /// The best matching intent in this result set.
        /// </summary>
        public Intent TopScoringIntent { get; set; }

        /// <summary>
        /// contains the dialog prompt and context (if exists)
        /// </summary>
        public Dialog DialogResponse { get; set; }

        /// <summary>
        /// List of <see cref="Intent"/> objects parsed.
        /// </summary>
        public Intent[] Intents { get; set; }

        /// <summary>
        /// Checks whether the result is awaiting more dialog or not
        /// </summary>
        /// <returns>boolean indicating whether the LuisResult awaits a dialog or not</returns>
        public bool isAwaitingDialogResponse()
        {
            return (DialogResponse != null &&
                string.Compare(DialogResponse.Status, DialogStatus.Finished, StringComparison.OrdinalIgnoreCase) != 0);
        }

        /// <summary>
        /// Collection of <see cref="Entity"/> objects parsed accessed though a dictionary for easy access.
        /// </summary>
        public IDictionary<string, IList<Entity>> Entities { get; set; }

        /// <summary>
        /// Collection of <see cref="CompositeEntities"/> objects parsed accessed though a dictionary for easy access.
        /// </summary>
        public IDictionary<string, IList<CompositeEntity>> CompositeEntities { get; set; }

        /// <summary>
        /// Construct an empty result set.
        /// </summary>
        public LuisResult() { }

        /// <summary>
        /// Contruce a result set based on the JSON response from the LUIS service.
        /// </summary>
        /// <param name="client">The client of which we can use to Reply</param>
        /// <param name="result">The parsed JSON from the LUIS service.</param>
        public LuisResult(JToken result)
        {
            Load(result);
        }

        /// <summary>
        /// Loads parsing results into this result set from the <see cref="JObject"/>.
        /// </summary>
        /// <param name="result"></param>
        public void Load(JToken result)
        {
            if (result == null) throw new ArgumentNullException("result");

            OriginalQuery = (string)result["query"] ?? string.Empty;
            var intents = (JArray)result["intents"] ?? new JArray();
            Intents = ParseIntentArray(intents);
            if (Intents.Length == 0)
            {
                var t = new Intent();
                t.Load((JObject)result["topScoringIntent"]);
                TopScoringIntent = t;
                Intents = new Intent[1];
                Intents[0] = TopScoringIntent;
            }
            else
            {
                TopScoringIntent = Intents[0];
            }
            if (result["dialog"] != null)
            {
                var t = new Dialog();
                t.Load((JObject)result["dialog"]);
                DialogResponse = t;
            }
            var entities = (JArray)result["entities"] ?? new JArray();
            Entities = ParseEntityArrayToDictionary(entities);
            var compositeEntities = (JArray)result["compositeEntities"] ?? new JArray();
            CompositeEntities = ParseCompositeEntityArrayToDictionary(compositeEntities);
        }

        /// <summary>
        /// gets all entities returned by the LUIS service
        /// </summary>
        /// <returns>a list of all entities</returns>
        public List<Entity> GetAllEntities()
        {
            List<Entity> entities = new List<Entity>();
            foreach (var entityList in Entities)
            {
                foreach (Entity entity in entityList.Value)
                {
                    entities.Add(entity);
                }
            }
            return entities;
        }

        /// <summary>
        /// gets all composite entities returned by the LUIS service
        /// </summary>
        /// <returns>a list of all composite entities</returns>
        public List<CompositeEntity> GetAllCompositeEntities()
        {
            List<CompositeEntity> compositeEntities = new List<CompositeEntity>();
            foreach (var compositeEntityList in CompositeEntities)
            {
                foreach (CompositeEntity compositeEntity in compositeEntityList.Value)
                {
                    compositeEntities.Add(compositeEntity);
                }
            }
            return compositeEntities;
        }

        /// <summary>
        /// Parses a json array of intents into an intent array
        /// </summary>
        /// <param name="array">Json array containing intents</param>
        /// <returns>Intent array</returns>
        private Intent[] ParseIntentArray(JArray array)
        {
            var count = array.Count;
            var a = new Intent[count];
            for (var i = 0; i < count; i++)
            {
                var t = new Intent();
                t.Load((JObject)array[i]);
                a[i] = t;
            }

            return a;
        }

        /// <summary>
        /// Parses a json array of entities into an entity dictionary.
        /// </summary>
        /// <param name="array"></param>
        /// <returns>The object containing the dictionary of entities</returns>
        private IDictionary<string, IList<Entity>> ParseEntityArrayToDictionary(JArray array)
        {
            var count = array.Count;
            var dict = new Dictionary<string, IList<Entity>>();

            foreach (var item in array)
            {
                var e = new Entity();
                e.Load((JObject)item);

                IList<Entity> entityList;
                if (!dict.TryGetValue(e.Name, out entityList))
                {
                    dict[e.Name] = new List<Entity>() { e };
                }
                else
                {
                    entityList.Add(e);
                }
            }

            return dict;
        }

        /// <summary>
        /// Parses a json array of composite entities into a composite entity dictionary.
        /// </summary>
        /// <param name="array"></param>
        /// <returns>The object containing the dictionary of composite entities</returns>
        private IDictionary<string, IList<CompositeEntity>> ParseCompositeEntityArrayToDictionary(JArray array)
        {
            var count = array.Count;
            var dict = new Dictionary<string, IList<CompositeEntity>>();

            foreach (var item in array)
            {
                var e = new CompositeEntity();
                e.Load((JObject)item);

                IList<CompositeEntity> compositeEntityList;
                if (!dict.TryGetValue(e.ParentType, out compositeEntityList))
                {
                    dict[e.ParentType] = new List<CompositeEntity>() { e };
                }
                else
                {
                    compositeEntityList.Add(e);
                }
            }

            return dict;
        }
    }


    /// <summary>
    /// Represents an intent identified by the LUIS service. 
    /// </summary>
    public class Intent
    {
        /// <summary>
        /// Name of the intent.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Confidence score of the intent match.
        /// </summary>
        public double Score { get; set; }

        /// <summary>
        /// Array of actions containing the actions 
        /// </summary>
        public Action[] Actions { get; set; }

        /// <summary>
        /// Load the intent values from JSON returned from the LUIS service.
        /// </summary>
        /// <param name="intent">JSON containing the intent values.</param>
        public void Load(JObject intent)
        {
            Name = (string)intent["intent"];
            Score = (double)intent["score"];
            var actions = (JArray)intent["actions"] ?? new JArray();
            Actions = ParseActionArray(actions);
        }

        /// <summary>
        /// Parses an json array of actions into an action object array
        /// </summary>
        /// <param name="array"></param>
        /// <returns>Array of Action of objects</returns>
        private Action[] ParseActionArray(JArray array)
        {
            var count = array.Count;
            var a = new Action[count];
            for (var i = 0; i < count; i++)
            {
                var t = new Action();
                t.Load((JObject)array[i]);
                a[i] = t;
            }
            return a;
        }
    }

    /// <summary
    /// Represents an entity recognised by LUIS
    /// </summary>
    public class Entity
    {
        /// <summary>
        /// The name of the type of Entity, e.g. "Topic", "Person", "Location".
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The entity value, e.g. "Latest scores", "Alex", "Cambridge".
        /// </summary>
        public string Value { get; set; }
        /// <summary>
        /// Confidence score that LUIS matched the entity, the higher the better.
        /// </summary>
        public double Score { get; set; }
        /// <summary>
        /// The index of the first character of the entity within the given text
        /// </summary>
        public int StartIndex { get; set; }
        /// <summary>
        /// The index of the last character of the entity within the given text
        /// </summary>
        public int EndIndex { get; set; }
        /// <summary>
        /// The resolution dictionary containing specific parameters for built-in entities
        /// </summary>
        public Dictionary<string, Object> Resolution;

        /// <summary>
        /// Loads the Entity values from a JSON object returned from the LUIS service.
        /// </summary>
        /// <param name="entity">The JObject containing the entity values</param>
        public void Load(JObject entity)
        {
            Name = (string)entity["type"];
            Value = (string)entity["entity"];
            try
            {
                Score = (double)entity["score"];
            }
            catch (Exception)
            {
                Score = -1;
            }
            try
            {
                StartIndex = (int)entity["startIndex"];
            }
            catch (Exception)
            {
                StartIndex = -1;
            }
            try
            {
                EndIndex = (int)entity["endIndex"];
            }
            catch (Exception)
            {
                EndIndex = -1;
            }
            try
            {
                Resolution = entity["resolution"].ToObject<Dictionary<string, Object>>();
            }
            catch (Exception)
            {
                Resolution = new Dictionary<string, Object>();
            }
        }
    }

        public class Action
        {
            /// <summary>
            /// Name of the action.
            /// </summary>
            public string Name { get; set; }
            /// <summary>
            /// Where the action is triggered or not
            /// </summary>
            public bool Triggered { get; set; }

            public Parameter[] Parameters { get; set; }
            /// <summary>
            /// Load the action values from JSON returned from the LUIS service.
            /// </summary>
            /// <param name="action">JSON containing the intent values.</param>
            public void Load(JObject action)
            {
                Name = (string)action["name"];
                Triggered = (bool)action["triggered"];
                var parameterss = (JArray)action["parameters"] ?? new JArray();
                Parameters = ParseParamArray(parameterss);
            }
            /// <summary>
            /// Parses an array of paramaeters from a json object into a parameter object array
            /// </summary>
            /// <param name="array">Json array containing the parameters</param>
            /// <returns>Parameter object array</returns>
            private Parameter[] ParseParamArray(JArray array)
            {
                var count = array.Count;
                var a = new Parameter[count];
                for (var i = 0; i < count; i++)
                {
                    var t = new Parameter();
                    t.Load((JObject)array[i]);
                    a[i] = t;
                }
                return a;
            }
        }

    public class Parameter
    {
        /// <summary>
        /// Name of the parameter.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// whether the parameter is required or not
        /// </summary>
        public bool Required { get; set; }

        /// <summary>
        /// list of parameter values (entities) in the parameter
        /// </summary>
        public ParameterValue[] ParameterValues { get; set; }

        /// <summary>
        /// Loads the json object into the properties of the object.
        /// </summary>
        /// <param name="parameterValue">Json object containing the parameter value</param>
        public void Load(JObject parameter)
        {
            Name = (string)parameter["name"];
            Required = (bool)parameter["required"];
            try
            {
                var values = (JArray)parameter["value"] ?? new JArray();
                ParameterValues = ParseValuesArray(values);
            }
            catch (Exception)
            {
                ParameterValues = null;
            }

        }

        /// <summary>
        /// Parses Json array of parameter values into parameter value array.
        /// </summary>
        /// <param name="array"></param>
        /// <returns>parameter value array</returns>
        private ParameterValue[] ParseValuesArray(JArray array)
        {
            var count = array.Count;
            var a = new ParameterValue[count];
            for (var i = 0; i < count; i++)
            {
                var t = new ParameterValue();
                t.Load((JObject)array[i]);
                a[i] = t;
            }
            return a;
        }
    }


    public class ParameterValue
    {

        /// <summary>
        /// The entity detected
        /// </summary>
        public string Entity { get; set; }
        /// <summary>
        /// The type of entity
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// The resolution dictionary containing specific parameters for built-in entities
        /// </summary>
        public Dictionary<string, Object> Resolution;

        /// <summary>
        /// Loads the json object into the properties of the object
        /// </summary>
        /// <param name="parameterValue">Json object containing the parameter value</param>
        public void Load(JObject parameterValue)
        {
            Entity = (string)parameterValue["entity"];
            Type = (string)parameterValue["type"];
            try
            {
                Resolution = parameterValue["resolution"].ToObject<Dictionary<string, Object>>();
            }
            catch (Exception)
            {
                Resolution = new Dictionary<string, Object>();
            }
        }
    }

    /// <summary
    /// Represents a composite entity recognised by LUIS
    /// </summary>
    public class CompositeEntity
    {
        /// <summary>
        /// The name of the type of parent entity.
        /// </summary>
        public string ParentType { get; set; }
        /// <summary>
        /// The composite entity value.
        /// </summary>
        public string Value { get; set; }
        /// <summary>
        /// A list of child entities of the composite entity.
        /// </summary>
        public CompositeEntityChild[] CompositeEntityChildren { get; set; }

        /// <summary>
        /// Loads the json object into the properties of the object.
        /// </summary>
        /// <param name="compositeEntity">Json object containing the composite entity</param>
        public void Load(JObject compositeEntity)
        {
            ParentType = (string)compositeEntity["parentType"];
            Value = (string)compositeEntity["value"];
            try
            {
                var values = (JArray)compositeEntity["children"] ?? new JArray();
                CompositeEntityChildren = ParseValuesArray(values);
            }
            catch (Exception)
            {
                CompositeEntityChildren = null;
            }

        }

        /// <summary>
        /// Parses Json array of composite entity children into composite entity child array.
        /// </summary>
        /// <param name="array"></param>
        /// <returns>entities array</returns>
        private CompositeEntityChild[] ParseValuesArray(JArray array)
        {
            var count = array.Count;
            var a = new CompositeEntityChild[count];
            for (var i = 0; i < count; i++)
            {
                var t = new CompositeEntityChild();
                t.Load((JObject)array[i]);
                a[i] = t;
            }
            return a;
        }
    }

    /// <summary
    /// Represents a composite entity recognised by LUIS
    /// </summary>
    public class CompositeEntityChild
    {
        /// <summary>
        /// The name of the type of parent entity.
        /// </summary>
        public string ParentType { get; set; }
        /// <summary>
        /// The composite entity value.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Loads the json object into the properties of the object.
        /// </summary>
        /// <param name="compositeEntityChild">Json object containing the composite entity child</param>
        public void Load(JObject compositeEntityChild)
        {
            ParentType = (string)compositeEntityChild["type"];
            Value = (string)compositeEntityChild["value"];
        }
    }

    public class Dialog
    {
        /// <summary>
        /// The question asked by the LUIS service for the next Reply
        /// </summary>
        public string Prompt { get; set; }
        /// <summary>
        /// Entity type of the required parameter
        /// </summary>
        public string ParameterName { get; set; }

        /// <summary>
        /// Context ID to send to the LUIS service for the state of the question
        /// </summary>
        public string ContextId { get; set; }

        /// <summary>
        /// status of the response, whether its a question or finished
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Loads the json object into the properties of the object
        /// </summary>
        /// <param name="dialog">Json object containing the dialog response</param>
        public void Load(JObject dialog)
        {
            Prompt = (string)dialog["prompt"];
            ParameterName = (string)dialog["parameterName"];
            ContextId = (string)dialog["contextId"];
            Status = (string)dialog["status"];
        }
    }

    /// <summary>
    /// Represents possible statuses for dialog
    /// </summary>
    public static class DialogStatus
    {
        public const string Finished = "Finished";
        public const string Question = "Question";
    }

}