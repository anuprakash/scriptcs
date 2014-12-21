﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Common.Logging;
using Roslyn.Scripting;
using ScriptCs.Contracts;

namespace ScriptCs.Engine.Roslyn
{
    public class RoslynReplEngine : RoslynScriptEngine, IReplEngine
    {
        public RoslynReplEngine(IScriptHostFactory scriptHostFactory, ILog logger) : base(scriptHostFactory, logger)
        {
        }

        public ICollection<string> GetLocalVariables(ScriptPackSession scriptPackSession)
        {
                var variables = new Collection<string>();
                if (scriptPackSession != null && scriptPackSession.State.ContainsKey(SessionKey))
                {
                    var sessionState  = (SessionState<Session>)scriptPackSession.State[SessionKey];
                    var submissionObjectField = sessionState.Session.GetType()
                        .GetField("submissions", BindingFlags.Instance | BindingFlags.NonPublic);

                    if (submissionObjectField != null)
                    {
                        var submissionObjectFieldValue = submissionObjectField.GetValue(sessionState.Session);
                        if (submissionObjectFieldValue != null)
                        {
                            var submissionObjects = submissionObjectFieldValue as object[];

                            if (submissionObjects != null && submissionObjects.Any(x => x != null))
                            {
                                var processedFields = new Collection<string>();
                                foreach (var submissionObject in submissionObjects.Where(x => x != null).Reverse()) //reversing to get the latest submission first
                                {
                                    var fields =
                                        submissionObject.GetType()
                                            .GetFields()
                                            .Where(x => x.Name.ToLowerInvariant() != "<host-object>");
                                    foreach (var field in fields)
                                    {
                                        if (!processedFields.Contains(field.Name))
                                        {
                                            variables.Add(string.Format("{0} {1} = {2}", field.FieldType, field.Name,
                                                field.GetValue(submissionObject)));
                                            processedFields.Add(field.Name);
                                        }
                                    }
                                }
                            }
                        }

                    }
                }

                return variables;
        }

        protected override ScriptResult Execute(string code, Session session)
        {
            Guard.AgainstNullArgument("session", session);

            return string.IsNullOrWhiteSpace(FileName) && !IsCompleteSubmission(code)
                ? ScriptResult.Incomplete
                : base.Execute(code, session);
        }
    }
}