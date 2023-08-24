using System;
using System.Collections;

namespace RedLoader
{
    public class MelonCoroutines
    {
        /// <summary>
        /// Start a new coroutine.<br />
        /// Coroutines are called at the end of the game Update loops.
        /// </summary>
        /// <param name="routine">The target routine</param>
        /// <returns>An object that can be passed to Stop to stop this coroutine</returns>
        public static object Start(IEnumerator routine)
        {
            if (SupportModule.Interface == null)
                throw new NotSupportedException("Support module must be initialized before starting coroutines");
            return SupportModule.Interface.StartCoroutine(routine);
        }

        /// <summary>
        /// Stop a currently running coroutine
        /// </summary>
        /// <param name="coroutineToken">The coroutine to stop</param>
        public static void Stop(object coroutineToken)
        {
            if (SupportModule.Interface == null)
                throw new NotSupportedException("Support module must be initialized before starting coroutines");
            SupportModule.Interface.StopCoroutine(coroutineToken);
        }
        
        public readonly struct CoroutineToken
        {
            private readonly object _token;
            
            public bool IsValid => _token != null;

            public CoroutineToken(object token)
            {
                _token = token;
            }

            public void Stop()
            {
                if (!IsValid)
                    return;

                MelonCoroutines.Stop(_token);
            }
        }
    }

    public static class CoroutineExtensions
    {
        public static MelonCoroutines.CoroutineToken RunCoro(this IEnumerator coro)
        {
            return new MelonCoroutines.CoroutineToken(MelonCoroutines.Start(coro));
        }
    }
}