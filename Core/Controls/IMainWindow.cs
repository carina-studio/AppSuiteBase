using System;
using System.ComponentModel;
using System.Windows.Input;

namespace CarinaStudio.AppSuite.Controls
{
    /// <summary>
    /// Main window interface.
    /// </summary>
    public interface IMainWindow : INotifyPropertyChanged
    {
        /// <summary>
        /// Cancel pending window size saving.
        /// </summary>
        void CancelSavingSize();


        /// <summary>
        /// Check whether multiple main windows were opened or not.
        /// </summary>
        bool HasMultipleMainWindows { get; }


        /// <summary>
        /// Command to layout main windows.
        /// </summary>
        /// <remarks>Type of parameter is <see cref="MultiWindowLayout"/>.</remarks>
        ICommand LayoutMainWindowsCommand { get; }
    }
}