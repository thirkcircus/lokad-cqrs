using System;
using System.Collections.Generic;
using System.Linq;
using Sample.Tests;

namespace Sample.Aggregates
{
    public sealed class AggregateAssertion<T> : Assertion<IEvent<T>[]> where T : IIdentity
    {
        readonly List<IEvent<T>> _expected;

        public AggregateAssertion(List<IEvent<T>> expected)
        {
            _expected = expected;
        }

        public IEnumerable<ExpectationResult> Assert(object fromWhen)
        {
                
            var actual = ((IEvent<T>[])fromWhen);
            // structurally equal comparison



            for (int i = 0; i < _expected.Count; i++)
            {
                var expectedHumanReadable = Describe.Object(_expected[i]);
                if (actual.Length > i)
                {
                    var diffs = CompareObjects.FindDifferences(_expected[i], actual[i]);
                    if (string.IsNullOrEmpty(diffs))
                    {
                        yield return new ExpectationResult
                            {
                                Passed = true, Text = expectedHumanReadable
                            };
                    }
                    else
                    {
                        var actualHumanReadable = Describe.Object(actual[i]);

                        if (actualHumanReadable != expectedHumanReadable)
                        {
                            // there is a difference in textual representations
                            yield return new ExpectationResult
                            {
                                Passed = false,
                                Text = expectedHumanReadable,
                                Exception = new InvalidOperationException("Was: " + actualHumanReadable)
                            };
                        }
                        else
                        {
                            
                            yield return new ExpectationResult
                            {
                                Passed = false,
                                Text = expectedHumanReadable,
                                Exception = new InvalidOperationException(diffs)
                            };
                        }


                    }
                }
                else
                {

                    yield return new ExpectationResult()
                        {
                            Passed = false,
                            Text = expectedHumanReadable,
                            Exception = new InvalidOperationException("Missing")
                        };

                }
            }

            for (int i = _expected.Count; i < actual.Count(); i++)
            {
                yield return new ExpectationResult()
                    {
                        Passed = false,
                        Text = "Unexpected message",
                        Exception = new InvalidOperationException("Was: " + Describe.Object(actual[i]))
                    };
            }
        }
    }
}