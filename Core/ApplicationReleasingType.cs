using System;

namespace CarinaStudio.AppSuite
{
    /// <summary>
    /// Type of application releasing.
    /// </summary>
    public enum ApplicationReleasingType
    {
        /// <summary>
        /// Stable release.
        /// </summary>
        Stable,
        /// <summary>
        /// Release Candidate.
        /// </summary>
        ReleaseCandidate,
        /// <summary>
        /// Preview release.
        /// </summary>
        Preview,
        /// <summary>
        /// Development release.
        /// </summary>
        Development,
    }
}
