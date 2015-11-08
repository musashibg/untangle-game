/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 * 
 * Project:	Untangle
 * 
 * Author:	Aleksandar Dalemski, a_dalemski@yahoo.com
 */

using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shapes;
using Untangle.ViewModels;

namespace Untangle
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml: Main window of the Untangle game application.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// The main window's view model instance.
        /// </summary>
        private readonly Main _viewModel;

        /// <summary>
        /// Initializes a new <see cref="MainWindow"/> instance and obtains its view model from the
        /// <see cref="System.Windows.FrameworkElement.DataContext"/> of the window.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            _viewModel = (Main)DataContext;
        }

        /// <summary>
        /// Handles the <see cref="System.Windows.UIElement.MouseMove"/> event of the Main window.
        /// </summary>
        /// <param name="sender">The object which raised the event.</param>
        /// <param name="e">The event's arguments.</param>
        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            Point position = e.GetPosition(ic_GameField);
            position.Offset(-ic_GameField.ActualWidth * 0.5, -ic_GameField.ActualHeight * 0.5);
            _viewModel.HandleMouseMove(position, (e.LeftButton == MouseButtonState.Pressed));
        }

        /// <summary>
        /// Handles the <see cref="System.Windows.UIElement.MouseUp"/> event of the Main window.
        /// </summary>
        /// <param name="sender">The object which raised the event.</param>
        /// <param name="e">The event's arguments.</param>
        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                _viewModel.HandleMouseUp();
        }

        /// <summary>
        /// Handles the <see cref="System.Windows.Window.Closing"/> event of the Main window.
        /// </summary>
        /// <param name="sender">The object which raised the event.</param>
        /// <param name="e">The event's arguments.</param>
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = !_viewModel.PromptForSaveGame();
        }

        /// <summary>
        /// Handles the <see cref="System.Windows.Input.CommandBinding.Executed"/> event of the
        /// command binding for the New game command.
        /// </summary>
        /// <param name="sender">The object which raised the event.</param>
        /// <param name="e">The event's arguments.</param>
        private void NewGameCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            _viewModel.NewGame();
        }

        /// <summary>
        /// Handles the <see cref="System.Windows.Input.CommandBinding.Executed"/> event of the
        /// command binding for the Save game command.
        /// </summary>
        /// <param name="sender">The object which raised the event.</param>
        /// <param name="e">The event's arguments.</param>
        private void SaveGameCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            _viewModel.SaveGame(true);
        }

        /// <summary>
        /// Handles the <see cref="System.Windows.Input.CommandBinding.Executed"/> event of the
        /// command binding for the Load game command.
        /// </summary>
        /// <param name="sender">The object which raised the event.</param>
        /// <param name="e">The event's arguments.</param>
        private void LoadGameCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            _viewModel.LoadGame();
        }

        /// <summary>
        /// Handles the <see cref="System.Windows.Input.CommandBinding.Executed"/> event of the
        /// command binding for the Exit command.
        /// </summary>
        /// <param name="sender">The object which raised the event.</param>
        /// <param name="e">The event's arguments.</param>
        private void ExitCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Handles the <see cref="System.Windows.Input.CommandBinding.Executed"/> event of the
        /// command binding for the About command.
        /// </summary>
        /// <param name="sender">The object which raised the event.</param>
        /// <param name="e">The event's arguments.</param>
        private void AboutCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var aboutBox = new AboutBox
            {
                Owner = this,
            };
            aboutBox.ShowDialog();
        }

        /// <summary>
        /// Handles the <see cref="System.Windows.Input.CommandBinding.Executed"/> event of the
        /// command binding for the Language choice command.
        /// </summary>
        /// <param name="sender">The object which raised the event.</param>
        /// <param name="e">The event's arguments.</param>
        private void LanguageCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            _viewModel.LanguageManager.SelectLanguage((string)e.Parameter);
        }

        /// <summary>
        /// Handles the <see cref="System.Windows.UIElement.MouseEnter"/> event of a vertex
        /// <see cref="System.Windows.Shapes.Ellipse"/> in the game field.
        /// </summary>
        /// <param name="sender">The object which raised the event.</param>
        /// <param name="e">The event's arguments.</param>
        private void Vertex_MouseEnter(object sender, MouseEventArgs e)
        {
            var ellipse = (Ellipse)sender;
            var vertex = (ViewModels.Vertex)ellipse.DataContext;
            _viewModel.HandleVertexMouseEnter(vertex);
        }

        /// <summary>
        /// Handles the <see cref="System.Windows.UIElement.MouseLeave"/> event of a vertex
        /// <see cref="System.Windows.Shapes.Ellipse"/> in the game field.
        /// </summary>
        /// <param name="sender">The object which raised the event.</param>
        /// <param name="e">The event's arguments.</param>
        private void Vertex_MouseLeave(object sender, MouseEventArgs e)
        {
            var ellipse = (Ellipse)sender;
            var vertex = (ViewModels.Vertex)ellipse.DataContext;
            _viewModel.HandleVertexMouseLeave(vertex);
        }

        /// <summary>
        /// Handles the <see cref="System.Windows.UIElement.MouseDown"/> event of a vertex
        /// <see cref="System.Windows.Shapes.Ellipse"/> in the game field.
        /// </summary>
        /// <param name="sender">The object which raised the event.</param>
        /// <param name="e">The event's arguments.</param>
        private void Vertex_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                var ellipse = (Ellipse)sender;
                var vertex = (ViewModels.Vertex)ellipse.DataContext;
                _viewModel.HandleVertexMouseDown(vertex);
            }
        }
    }
}