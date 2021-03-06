// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security
{
    [System.Runtime.InteropServices.ComVisible(true)]
    public class VerificationException : Exception
    {
        public VerificationException()
            : base(SR.Verification_Exception)
        {
            SetErrorCode(__HResults.COR_E_VERIFICATION);
        }

        public VerificationException(String message)
            : base(message)
        {
            SetErrorCode(__HResults.COR_E_VERIFICATION);
        }

        public VerificationException(String message, Exception innerException)
            : base(message, innerException)
        {
            SetErrorCode(__HResults.COR_E_VERIFICATION);
        }
    }
}
