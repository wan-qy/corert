// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// AggregateException.cs
//

//
// Public type to communicate multiple failures to an end-user.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Security;
using System.Text;
using System.Threading;

namespace System
{
    /// <summary>Represents one or more errors that occur during application execution.</summary>
    /// <remarks>
    /// <see cref="AggregateException"/> is used to consolidate multiple failures into a single, throwable
    /// exception object.
    /// </remarks>
    [DebuggerDisplay("Count = {InnerExceptionCount}")]
    public class AggregateException : Exception
    {
        private ReadOnlyCollection<Exception> m_innerExceptions; // Complete set of exceptions.

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateException"/> class.
        /// </summary>
        public AggregateException()
            : base(SR.AggregateException_ctor_DefaultMessage)
        {
            m_innerExceptions = new ReadOnlyCollection<Exception>(Array.Empty<Exception>());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateException"/> class with
        /// a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public AggregateException(string message)
            : base(message)
        {
            m_innerExceptions = new ReadOnlyCollection<Exception>(Array.Empty<Exception>());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateException"/> class with a specified error
        /// message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="innerException"/> argument
        /// is null.</exception>
        public AggregateException(string message, Exception innerException)
            : base(message, innerException)
        {
            if (innerException == null)
            {
                throw new ArgumentNullException("innerException");
            }

            m_innerExceptions = new ReadOnlyCollection<Exception>(new Exception[] { innerException });
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateException"/> class with
        /// references to the inner exceptions that are the cause of this exception.
        /// </summary>
        /// <param name="innerExceptions">The exceptions that are the cause of the current exception.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="innerExceptions"/> argument
        /// is null.</exception>
        /// <exception cref="T:System.ArgumentException">An element of <paramref name="innerExceptions"/> is
        /// null.</exception>
        public AggregateException(IEnumerable<Exception> innerExceptions) :
            this(SR.AggregateException_ctor_DefaultMessage, innerExceptions)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateException"/> class with
        /// references to the inner exceptions that are the cause of this exception.
        /// </summary>
        /// <param name="innerExceptions">The exceptions that are the cause of the current exception.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="innerExceptions"/> argument
        /// is null.</exception>
        /// <exception cref="T:System.ArgumentException">An element of <paramref name="innerExceptions"/> is
        /// null.</exception>
        public AggregateException(params Exception[] innerExceptions) :
            this(SR.AggregateException_ctor_DefaultMessage, innerExceptions)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateException"/> class with a specified error
        /// message and references to the inner exceptions that are the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerExceptions">The exceptions that are the cause of the current exception.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="innerExceptions"/> argument
        /// is null.</exception>
        /// <exception cref="T:System.ArgumentException">An element of <paramref name="innerExceptions"/> is
        /// null.</exception>
        public AggregateException(string message, IEnumerable<Exception> innerExceptions)
            // If it's already an IList, pass that along (a defensive copy will be made in the delegated ctor).  If it's null, just pass along
            // null typed correctly.  Otherwise, create an IList from the enumerable and pass that along. 
            : this(message, innerExceptions as IList<Exception> ?? (innerExceptions == null ? (IList<Exception>)null : new LowLevelListWithIList<Exception>(innerExceptions)))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateException"/> class with a specified error
        /// message and references to the inner exceptions that are the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerExceptions">The exceptions that are the cause of the current exception.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="innerExceptions"/> argument
        /// is null.</exception>
        /// <exception cref="T:System.ArgumentException">An element of <paramref name="innerExceptions"/> is
        /// null.</exception>
        public AggregateException(string message, params Exception[] innerExceptions) :
            this(message, (IList<Exception>)innerExceptions)
        {
        }

        /// <summary>
        /// Allocates a new aggregate exception with the specified message and list of inner exceptions.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerExceptions">The exceptions that are the cause of the current exception.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="innerExceptions"/> argument
        /// is null.</exception>
        /// <exception cref="T:System.ArgumentException">An element of <paramref name="innerExceptions"/> is
        /// null.</exception>
        private AggregateException(string message, IList<Exception> innerExceptions)
            : base(message, innerExceptions != null && innerExceptions.Count > 0 ? innerExceptions[0] : null)
        {
            if (innerExceptions == null)
            {
                throw new ArgumentNullException("innerExceptions");
            }

            // Copy exceptions to our internal array and validate them. We must copy them,
            // because we're going to put them into a ReadOnlyCollection which simply reuses
            // the list passed in to it. We don't want callers subsequently mutating.
            Exception[] exceptionsCopy = new Exception[innerExceptions.Count];

            for (int i = 0; i < exceptionsCopy.Length; i++)
            {
                exceptionsCopy[i] = innerExceptions[i];

                if (exceptionsCopy[i] == null)
                {
                    throw new ArgumentException(SR.AggregateException_ctor_InnerExceptionNull);
                }
            }

            m_innerExceptions = new ReadOnlyCollection<Exception>(exceptionsCopy);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateException"/> class with
        /// references to the inner exception dispatch info objects that represent the cause of this exception.
        /// </summary>
        /// <param name="innerExceptionInfos">
        /// Information about the exceptions that are the cause of the current exception.
        /// </param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="innerExceptionInfos"/> argument
        /// is null.</exception>
        /// <exception cref="T:System.ArgumentException">An element of <paramref name="innerExceptionInfos"/> is
        /// null.</exception>
        internal AggregateException(IEnumerable<ExceptionDispatchInfo> innerExceptionInfos) :
            this(SR.AggregateException_ctor_DefaultMessage, innerExceptionInfos)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateException"/> class with a specified error
        /// message and references to the inner exception dispatch info objects that represent the cause of 
        /// this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerExceptionInfos">
        /// Information about the exceptions that are the cause of the current exception.
        /// </param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="innerExceptionInfos"/> argument
        /// is null.</exception>
        /// <exception cref="T:System.ArgumentException">An element of <paramref name="innerExceptionInfos"/> is
        /// null.</exception>
        internal AggregateException(string message, IEnumerable<ExceptionDispatchInfo> innerExceptionInfos)
            // If it's already an IList, pass that along (a defensive copy will be made in the delegated ctor).  If it's null, just pass along
            // null typed correctly.  Otherwise, create an IList from the enumerable and pass that along. 
            : this(message, innerExceptionInfos as IList<ExceptionDispatchInfo> ??
                                (innerExceptionInfos == null ?
                                    (LowLevelListWithIList<ExceptionDispatchInfo>)null :
                                    new LowLevelListWithIList<ExceptionDispatchInfo>(innerExceptionInfos)))
        {
        }

        /// <summary>
        /// Allocates a new aggregate exception with the specified message and list of inner 
        /// exception dispatch info objects.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerExceptionInfos">
        /// Information about the exceptions that are the cause of the current exception.
        /// </param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="innerExceptionInfos"/> argument
        /// is null.</exception>
        /// <exception cref="T:System.ArgumentException">An element of <paramref name="innerExceptionInfos"/> is
        /// null.</exception>
        private AggregateException(string message, IList<ExceptionDispatchInfo> innerExceptionInfos)
            : base(message, innerExceptionInfos != null && innerExceptionInfos.Count > 0 && innerExceptionInfos[0] != null ?
                                innerExceptionInfos[0].SourceException : null)
        {
            if (innerExceptionInfos == null)
            {
                throw new ArgumentNullException("innerExceptionInfos");
            }

            // Copy exceptions to our internal array and validate them. We must copy them,
            // because we're going to put them into a ReadOnlyCollection which simply reuses
            // the list passed in to it. We don't want callers subsequently mutating.
            Exception[] exceptionsCopy = new Exception[innerExceptionInfos.Count];

            for (int i = 0; i < exceptionsCopy.Length; i++)
            {
                var edi = innerExceptionInfos[i];
                if (edi != null) exceptionsCopy[i] = edi.SourceException;

                if (exceptionsCopy[i] == null)
                {
                    throw new ArgumentException(SR.AggregateException_ctor_InnerExceptionNull);
                }
            }

            m_innerExceptions = new ReadOnlyCollection<Exception>(exceptionsCopy);
        }

        /// <summary>
        /// Returns the <see cref="System.AggregateException"/> that is the root cause of this exception.
        /// </summary>
        public override Exception GetBaseException()
        {
            // Returns the first inner AggregateException that contains more or less than one inner exception

            // Recursively traverse the inner exceptions as long as the inner exception of type AggregateException and has only one inner exception
            Exception back = this;
            AggregateException backAsAggregate = this;
            while (backAsAggregate != null && backAsAggregate.InnerExceptions.Count == 1)
            {
                back = back.InnerException;
                backAsAggregate = back as AggregateException;
            }
            return back;
        }

        /// <summary>
        /// Gets a read-only collection of the <see cref="T:System.Exception"/> instances that caused the
        /// current exception.
        /// </summary>
        public ReadOnlyCollection<Exception> InnerExceptions
        {
            get { return m_innerExceptions; }
        }


        /// <summary>
        /// Invokes a handler on each <see cref="T:System.Exception"/> contained by this <see
        /// cref="AggregateException"/>.
        /// </summary>
        /// <param name="predicate">The predicate to execute for each exception. The predicate accepts as an
        /// argument the <see cref="T:System.Exception"/> to be processed and returns a Boolean to indicate
        /// whether the exception was handled.</param>
        /// <remarks>
        /// Each invocation of the <paramref name="predicate"/> returns true or false to indicate whether the
        /// <see cref="T:System.Exception"/> was handled. After all invocations, if any exceptions went
        /// unhandled, all unhandled exceptions will be put into a new <see cref="AggregateException"/>
        /// which will be thrown. Otherwise, the <see cref="Handle"/> method simply returns. If any
        /// invocations of the <paramref name="predicate"/> throws an exception, it will halt the processing
        /// of any more exceptions and immediately propagate the thrown exception as-is.
        /// </remarks>
        /// <exception cref="AggregateException">An exception contained by this <see
        /// cref="AggregateException"/> was not handled.</exception>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="predicate"/> argument is
        /// null.</exception>
        public void Handle(Func<Exception, bool> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }

            LowLevelListWithIList<Exception> unhandledExceptions = null;
            for (int i = 0; i < m_innerExceptions.Count; i++)
            {
                // If the exception was not handled, lazily allocate a list of unhandled
                // exceptions (to be rethrown later) and add it.
                if (!predicate(m_innerExceptions[i]))
                {
                    if (unhandledExceptions == null)
                    {
                        unhandledExceptions = new LowLevelListWithIList<Exception>();
                    }

                    unhandledExceptions.Add(m_innerExceptions[i]);
                }
            }

            // If there are unhandled exceptions remaining, throw them.
            if (unhandledExceptions != null)
            {
                throw new AggregateException(Message, unhandledExceptions);
            }
        }


        /// <summary>
        /// Flattens an <see cref="AggregateException"/> instances into a single, new instance.
        /// </summary>
        /// <returns>A new, flattened <see cref="AggregateException"/>.</returns>
        /// <remarks>
        /// If any inner exceptions are themselves instances of
        /// <see cref="AggregateException"/>, this method will recursively flatten all of them. The
        /// inner exceptions returned in the new <see cref="AggregateException"/>
        /// will be the union of all of the the inner exceptions from exception tree rooted at the provided
        /// <see cref="AggregateException"/> instance.
        /// </remarks>
        public AggregateException Flatten()
        {
            // Initialize a collection to contain the flattened exceptions.
            LowLevelListWithIList<Exception> flattenedExceptions = new LowLevelListWithIList<Exception>();

            // Create a list to remember all aggregates to be flattened, this will be accessed like a FIFO queue
            LowLevelList<AggregateException> exceptionsToFlatten = new LowLevelList<AggregateException>();
            exceptionsToFlatten.Add(this);
            int nDequeueIndex = 0;

            // Continue removing and recursively flattening exceptions, until there are no more.
            while (exceptionsToFlatten.Count > nDequeueIndex)
            {
                // dequeue one from exceptionsToFlatten
                IList<Exception> currentInnerExceptions = exceptionsToFlatten[nDequeueIndex++].InnerExceptions;

                for (int i = 0; i < currentInnerExceptions.Count; i++)
                {
                    Exception currentInnerException = currentInnerExceptions[i];

                    if (currentInnerException == null)
                    {
                        continue;
                    }

                    AggregateException currentInnerAsAggregate = currentInnerException as AggregateException;

                    // If this exception is an aggregate, keep it around for later.  Otherwise,
                    // simply add it to the list of flattened exceptions to be returned.
                    if (currentInnerAsAggregate != null)
                    {
                        exceptionsToFlatten.Add(currentInnerAsAggregate);
                    }
                    else
                    {
                        flattenedExceptions.Add(currentInnerException);
                    }
                }
            }


            return new AggregateException(Message, flattenedExceptions);
        }

        /// <summary>Gets a message that describes the exception.</summary>
        public override string Message
        {
            get
            {
                if (m_innerExceptions.Count == 0)
                {
                    return base.Message;
                }

                StringBuilder sb = new StringBuilder();
                sb.Append(base.Message);
                sb.Append(' ');
                for (int i = 0; i < m_innerExceptions.Count; i++)
                {
                    sb.Append('(');
                    sb.Append(m_innerExceptions[i].Message);
                    sb.Append(") ");
                }
                sb.Length -= 1;
                return sb.ToString();
            }
        }

        /// <summary>
        /// Creates and returns a string representation of the current <see cref="AggregateException"/>.
        /// </summary>
        /// <returns>A string representation of the current exception.</returns>
        public override string ToString()
        {
            StringBuilder text = new StringBuilder();
            text.Append(base.ToString());

            for (int i = 0; i < m_innerExceptions.Count; i++)
            {
                text.Append(Environment.NewLine);
                text.Append("---> ");
                text.Append(string.Format(CultureInfo.InvariantCulture, SR.AggregateException_InnerException, i));
                text.Append(m_innerExceptions[i].ToString());
                text.Append("<---");
                text.Append(Environment.NewLine);
            }

            return text.ToString();
        }

        /// <summary>
        /// This helper property is used by the DebuggerDisplay.
        /// 
        /// Note that we don't want to remove this property and change the debugger display to {InnerExceptions.Count} 
        /// because DebuggerDisplay should be a single property access or parameterless method call, so that the debugger 
        /// can use a fast path without using the expression evaluator.
        /// 
        /// See http://msdn.microsoft.com/en-us/library/x810d419.aspx
        /// </summary>
        private int InnerExceptionCount
        {
            get
            {
                return InnerExceptions.Count;
            }
        }
    }
}
