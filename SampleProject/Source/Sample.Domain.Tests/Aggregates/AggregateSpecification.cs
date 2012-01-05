using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Sample.Tests;
using Sample.Tests.PAssert;
using Extensions = Sample.Tests.Extensions;

namespace Sample.Aggregates
{

    /// <summary>
    /// Just for grabbing the information about specifications
    /// </summary>
    public interface IAggregateSpecification
    {
        IEnumerable<IEvent<IIdentity>> GetExpect();
        IEnumerable<IEvent<IIdentity>> GetGiven();
        ICommand<IIdentity> GetWhen();
    }


    public class AggregateFailSpecification<T,TException> : 
        TypedSpecification<TException>, IAggregateSpecification
        
        where T : IIdentity 
        where  TException : Exception
    {
        public List<Expression<Action>> Before = new List<Expression<Action>>();
        public List<Expression<Func<TException, bool>>> Expect = new List<Expression<Func<TException, bool>>>();
        public Action Finally;
        public List<IEvent<T>> Given = new List<IEvent<T>>();
        public ICommand<T> When;
        public string Name;
        public delegate IAggregate<T> FactoryDelegate(IEnumerable<IEvent<T>> events, Action<IEvent<T>> observer);

        public FactoryDelegate Factory;


        public string GetName() { return Name; }
        public void Document(RunResult result)
        {
            PrintEvil.Document(result, Before, Given.ToArray(), When, _text.ToString());
        }

        public Action GetBefore()
        {
            return () =>
            {
                foreach (var expression in Before)
                {
                    expression.Compile()();
                }
            };
        }


        sealed class AggregateFeed
        {
            public readonly IList<IEvent<T>> Feed;
            public readonly IAggregate<T> Aggregate;

            public AggregateFeed(IList<IEvent<T>> feed, IAggregate<T> aggregate)
            {
                Feed = feed;
                Aggregate = aggregate;
            }
        }
        public Delegate GetOn()
        {
            return new Func<AggregateFeed>(() =>
            {
                var list = new List<IEvent<T>>();

                var aggregate = Factory(Given, list.Add);
                return new AggregateFeed(list, aggregate);
            });
        }

        readonly StringBuilder _text = new StringBuilder();

        IEnumerable<IEvent<IIdentity>> IAggregateSpecification.GetExpect()
        {
            return new IEvent<IIdentity>[0];
        }

        IEnumerable<IEvent<IIdentity>> IAggregateSpecification.GetGiven()
        {
            return Given.Cast<IEvent<IIdentity>>();
        }

        ICommand<IIdentity> IAggregateSpecification.GetWhen()
        {
            return (ICommand<IIdentity>) When;
        }

        public Delegate GetWhen()
        {
            return new Func<AggregateFeed, TException>(feed =>
            {
                try
                {
                    Context.SwapFor(s => _text.AppendLine(s));
                    feed.Aggregate.Execute(When);
                    return null;
                }
                    catch(TException ex)
                    {
                        Context.Explain(ex.Message);
                        return ex;
                    }
                finally
                {
                    Context.SwapFor();
                }
            });
        }

        public IEnumerable<Assertion<TException>> GetAssertions()
        {
            return Expect.Select(x => new PAssertion<TException>(x));
        }

        public Action GetFinally()
        {
            return this.Finally;
        }

 
    }

    public class AggregateSpecification<T> : TypedSpecification<IEvent<T>[]>, IAggregateSpecification
        where T : IIdentity
    {
        
        public List<Expression<Action>> Before = new List<Expression<Action>>();
        public List<IEvent<T>> Expect = new List<IEvent<T>>();
        public Action Finally;
        public List<IEvent<T>> Given = new List<IEvent<T>>();
        public ICommand<T> When;
        public string Name;

        public delegate IAggregate<T> FactoryDelegate(IEnumerable<IEvent<T>> events, Action<IEvent<T>> observer);

        public FactoryDelegate Factory;
        

        
        

        public string GetName() {  return Name; }

        public Action GetBefore()
        {
            return () =>
                {
                    foreach (var expression in Before)
                    {
                        expression.Compile()();
                    }
                };
        }

        public Delegate GetOn()
        {
            return new Func<AggregateFeed>(() =>
                {
                    var list = new List<IEvent<T>>();
                   
                    var aggregate = Factory(Given, list.Add);
                    return new AggregateFeed(list, aggregate);
                });
        }



        sealed class AggregateFeed
        {
            public readonly IList<IEvent<T>> Feed;
            public readonly IAggregate<T> Aggregate;

            public AggregateFeed(IList<IEvent<T>> feed, IAggregate<T> aggregate)
            {
                Feed = feed;
                Aggregate = aggregate;
            }
        }

        readonly StringBuilder _text = new StringBuilder();

        public Delegate GetWhen()
        {
            return new Func<AggregateFeed, IEvent<T>[]>(feed =>
                {
                    try
                    {
                        Context.SwapFor(s =>_text.AppendLine(s));
                        feed.Aggregate.Execute(When);
                        return feed.Feed.ToArray();
                    }
                    finally
                    {
                        Context.SwapFor();
                    }
                });
        }

        public IEnumerable<IEvent<IIdentity>> GetExpect()
        {
            return Expect.Cast<IEvent<IIdentity>>();
        }

        public IEnumerable<IEvent<IIdentity>> GetGiven()
        {
            return Given.Cast<IEvent<IIdentity>>();
        }

        ICommand<IIdentity> IAggregateSpecification.GetWhen()
        {
            return (ICommand<IIdentity>) When;
        }

        public IEnumerable<Assertion<IEvent<T>[]>> GetAssertions()
        {
            yield return new AggregateAssertion<T>(Expect);
        }

        public Action GetFinally()
        {
            return Finally;
        }



        public void Document(RunResult result)
        {
            PrintEvil.Document(result, Before, Given.ToArray(), When, _text.ToString());
        }
    }

    static class PrintEvil
    {
        public static void PrintAdjusted(string adj, string text)
        {
            bool first = true;
            foreach (var s in text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None))
            {

                Console.Write(first ? adj : new string(' ', adj.Length));
                Console.WriteLine(s);
                first = false;
            }
        }

        public static void Document<T>(
            RunResult result, 
            List<Expression<Action>> before,
            IEvent<T>[] given,
            ICommand<T> when,
            string decisions) where T : IIdentity
        {
            var passed = result.Passed ? "Passed" : "Failed";
            Console.WriteLine("{0}: {1} - {2}", Extensions.CleanupName(result.FoundOnMemberInfo.DeclaringType.Name), Extensions.CleanupName(result.Name), passed);

            if (before.Any())
            {
                Console.WriteLine();
                Console.WriteLine("Environment: ");
                foreach (var expression in before)
                {
                    PrintAdjusted("  ", PAssert.CreateSimpleFormatFor(expression));
                }
            }

            if (given.Any())
            {
                Console.WriteLine();
                Console.WriteLine("Given:");

                for (int i = 0; i < given.Length; i++)
                {
                    PrintEvil.PrintAdjusted(" " + (i + 1) + ". ", Describe.Object(given[i]).Trim());
                }
            }


            if (when != null)
            {
                Console.WriteLine();
                PrintAdjusted("When: ", Describe.Object(when).Trim());
            }

            Console.WriteLine();
            Console.WriteLine("Expectations:");
            foreach (var expecation in result.Expectations)
            {
                PrintAdjusted("  " + (expecation.Passed ? "[Passed]" : "[Failed]") + " ", expecation.Text.Trim());
                if (!expecation.Passed && expecation.Exception != null)
                {
                    PrintAdjusted("             ", expecation.Exception.Message);
                }
            }



            if (result.Thrown != null)
            {

                Console.WriteLine("Specification failed: " + result.Message.Trim());
                Console.WriteLine();
                Console.WriteLine(result.Thrown);
            }

            if (decisions.Length > 0)
            {
                Console.WriteLine("");
                Console.WriteLine("Decisions made:");
                PrintAdjusted("  ", decisions.ToString());
            }

            Console.WriteLine(new string('-', 80));
            Console.WriteLine();
        }
    }
}