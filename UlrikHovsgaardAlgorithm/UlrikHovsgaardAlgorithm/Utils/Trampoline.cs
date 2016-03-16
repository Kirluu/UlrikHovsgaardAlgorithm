using System;

namespace UlrikHovsgaardAlgorithm.Utils
{
    /// <summary>
    /// http://blog.functionalfun.net/2008/04/bouncing-on-your-tail.html
    /// 
    /// We may be able to use this to solve the recursion StackOverflowExceptions we encounter.
    /// </summary>
    public static class Trampoline
    {
        public static Func<T1, T2, TResult> MakeTrampoline<T1, T2, TResult>(Func<T1, T2, Bounce<T1, T2, TResult>> function)
        {
            Func<T1, T2, TResult> trampolined = (T1 arg1, T2 arg2) =>
            {
                T1 currentArg1 = arg1;
                T2 currentArg2 = arg2;

                while (true)
                {
                    Bounce<T1, T2, TResult> result = function(currentArg1, currentArg2);

                    if (result.HasResult)
                    {
                        return result.Result;
                    }
                    else
                    {
                        currentArg1 = result.Param1;
                        currentArg2 = result.Param2;
                    }
                }
            };

            return trampolined;
        }

        public static Action<T1, T2> MakeActionTrampoline<T1, T2>(Func<T1, T2, ActionBounce<T1, T2>> function)
        {
            Action<T1, T2> trampolined = (T1 arg1, T2 arg2) =>
            {
                T1 currentArg1 = arg1;
                T2 currentArg2 = arg2;

                while (true)
                {
                    ActionBounce<T1, T2> result = function(currentArg1, currentArg2);

                    if (result.Recurse)
                    {
                        currentArg1 = result.Param1;
                        currentArg2 = result.Param2;
                    }
                    else
                    {
                        return;
                    }
                }
            };

            return trampolined;
        }

        public static ActionBounce<T1, T2> RecurseAction<T1, T2>(T1 arg1, T2 arg2)
        {
            return new ActionBounce<T1, T2>(arg1, arg2);
        }

        public static ActionBounce<T1, T2> EndAction<T1, T2>()
        {
            return new ActionBounce<T1, T2>();
        }


        public static Bounce<T1, T2, TResult> Recurse<T1, T2, TResult>(T1 arg1, T2 arg2)
        {
            return new Bounce<T1, T2, TResult>(arg1, arg2);
        }

        public static Bounce<T1, T2, TResult> ReturnResult<T1, T2, TResult>(TResult result)
        {
            return new Bounce<T1, T2, TResult>(result);
        }

    }

    public struct ActionBounce<T1, T2>
    {
        public T1 Param1 { get; private set; }
        public T2 Param2 { get; private set; }

        public bool Recurse { get; private set; }

        public ActionBounce(T1 param1, T2 param2) : this()
        {
            Param1 = param1;
            Param2 = param2;

            Recurse = true;
        }
    }

    public struct Bounce<T1, T2, TResult>
    {
        public T1 Param1 { get; private set; }
        public T2 Param2 { get; private set; }

        public TResult Result { get; private set; }
        public bool HasResult { get; private set; }
        public bool Recurse { get; private set; }

        public Bounce(T1 param1, T2 param2) : this()
        {
            Param1 = param1;
            Param2 = param2;
            HasResult = false;

            Recurse = true;
        }

        public Bounce(TResult result) : this()
        {
            Result = result;
            HasResult = true;

            Recurse = false;
        }
    }
}
