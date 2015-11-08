/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 * 
 * Project:	Untangle
 * 
 * Author:	Aleksandar Dalemski, a_dalemski@yahoo.com
 */

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Untangle.Resources;
using Untangle.Saves;

namespace Untangle.ViewModels
{
    /// <summary>
    /// A view model class for the main UI of the Untangle game application.
    /// </summary>
    public class Main : ViewModelBase
    {
        /// <summary>
        /// Property name constant for the current game of Untangle loaded in the application.
        /// </summary>
        public const string GamePropertyName = "Game";
        /// <summary>
        /// Property name constant for the application's current title text.
        /// </summary>
        public const string TitlePropertyName = "Title";

        /// <summary>
        /// A command for displaying the About box of the application.
        /// </summary>
        public static ICommand AboutCommand = new RoutedCommand();

        /// <summary>
        /// A command for changing the selected language of the application.
        /// </summary>
        public static ICommand LanguageCommand = new RoutedCommand();

        /// <summary>
        /// The application's language manager.
        /// </summary>
        private readonly LanguageManager _languageManager;

        /// <summary>
        /// The current game of Untangle loaded in the application.
        /// </summary>
        private Game _game;
        /// <summary>
        /// Specifies whether a save game prompt should be displayed if the user is about to lose
        /// his current game progress.
        /// </summary>
        /// <remarks>
        /// <para>The save game prompt is not needed if the user has not dragged any vertices since
        /// the game was started or last saved.</para>
        /// </remarks>
        private bool _needSaveGamePrompt;

        /// <summary>
        /// The application's language manager.
        /// </summary>
        public LanguageManager LanguageManager
        {
            get { return _languageManager; }
        }

        /// <summary>
        /// The current game of Untangle loaded in the application.
        /// </summary>
        public Game Game
        {
            get { return _game; }
            set
            {
                if (_game == value)
                    return;

                if (_game != null)
                    _game.PropertyChanged -= Game_PropertyChanged;
                _game = value;
                if (_game != null)
                    _game.PropertyChanged += Game_PropertyChanged;
                OnPropertyChanged(GamePropertyName);
                OnPropertyChanged(TitlePropertyName);
            }
        }

        /// <summary>
        /// The application's current title text.
        /// </summary>
        public string Title
        {
            get
            {
                return string.Format(Resources.MainWindow.WindowTitleFormat, Game.LevelNumber);
            }
        }

        /// <summary>
        /// Initializes a new <see cref="Main"/> instance.
        /// </summary>
        public Main()
        {
            _languageManager = new LanguageManager();
            Game = new Game(1);
            _needSaveGamePrompt = false;

            _languageManager.PropertyChanged += LanguageManager_PropertyChanged;
        }

        /// <summary>
        /// Displays a save game prompt when the user is about to lose his current game progress,
        /// if needed.
        /// </summary>
        /// <returns><see langword="true"/> if the user has chosen to proceed with the operation
        /// after saving his current game or deliberately choosing not to save it.</returns>
        /// <remarks>
        /// <para>The save game prompt is not needed if the user has not dragged any vertices since
        /// the game was started or last saved.</para>
        /// </remarks>
        public bool PromptForSaveGame()
        {
            if (!_needSaveGamePrompt)
                return true;

            MessageBoxResult result = MessageBox.Show(
                Messages.SaveGamePrompt,
                MessageCaptions.SaveGamePrompt,
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning,
                MessageBoxResult.Yes);

            if (result == MessageBoxResult.Yes)
                return SaveGame(false);

            return (result != MessageBoxResult.Cancel);
        }

        /// <summary>
        /// Starts a new game of Untangle from scratch.
        /// </summary>
        /// <remarks>
        /// <para>The user will be prompted to save his current game progress, if needed.</para>
        /// </remarks>
        public void NewGame()
        {
            if (!PromptForSaveGame())
                return;

            Game = new Game(1);
            _needSaveGamePrompt = false;
        }

        /// <summary>
        /// Saves the current game of Untangle to a file chosen by the user.
        /// </summary>
        /// <param name="showSuccessMessage">Specifies whether a message should be displayed to the
        /// user when the game has been saved successfully.</param>
        /// <returns><see langword="true"/> if the game has been saved successfully.</returns>
        public bool SaveGame(bool showSuccessMessage)
        {
            try
            {
                if (SaveHelper.SaveGame(Game))
                {
                    if (showSuccessMessage)
                    {
                        MessageBox.Show(
                            Messages.SaveGameSuccess,
                            MessageCaptions.SaveGameSuccess);
                    }
                    _needSaveGamePrompt = false;
                    return true;
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    MessageCaptions.SaveGameError,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            return false;
        }

        /// <summary>
        /// Loads a saved game of Untangle from a file chosen by the user.
        /// </summary>
        /// <remarks>
        /// <para>The user will be prompted to save his current game progress, if needed.</para>
        /// </remarks>
        public void LoadGame()
        {
            if (!PromptForSaveGame())
                return;

            try
            {
                Game game;
                if (!SaveHelper.LoadGame(out game))
                    return;
                Game = game;
                _needSaveGamePrompt = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    MessageCaptions.LoadGameError,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles the event of the mouse being moved over the game field.
        /// </summary>
        /// <param name="position">The new position of the mouse on the game field.</param>
        /// <param name="buttonPressed">Specifies whether the left mouse button is currently
        /// pressed.</param>
        public void HandleMouseMove(Point position, bool buttonPressed)
        {
            if (Game.Level.IsDragging)
            {
                _needSaveGamePrompt = true;
                if (buttonPressed)
                    Game.Level.DragVertex(position);
                else
                    Game.Level.FinishDrag();
            }
        }

        /// <summary>
        /// Handles the event of the left mouse button being released over the game field.
        /// </summary>
        public void HandleMouseUp()
        {
            if (Game.Level.IsDragging)
                Game.Level.FinishDrag();
        }

        /// <summary>
        /// Handles the event of the mouse cursor entering the area of a vertex on the game field.
        /// </summary>
        /// <param name="vertex">The vertex whose area has been entered by the mouse cursor.
        /// </param>
        public void HandleVertexMouseEnter(Vertex vertex)
        {
            Game.Level.SetVertexUnderMouse(vertex);
        }

        /// <summary>
        /// Handles the event of the mouse cursor leaving the area of a vertex on the game field.
        /// </summary>
        /// <param name="vertex">The vertex whose area has been left by the mouse cursor.</param>
        public void HandleVertexMouseLeave(Vertex vertex)
        {
            Game.Level.SetVertexUnderMouse(null);
        }

        /// <summary>
        /// Handles the event of the left mouse button being pressed over a vertex on the game
        /// field.
        /// </summary>
        /// <param name="vertex">The vertex over which the left mouse button has been pressed.
        /// </param>
        public void HandleVertexMouseDown(Vertex vertex)
        {
            Game.Level.StartDrag(vertex);
        }

        /// <summary>
        /// Handles the <see cref="ViewModelBase.PropertyChanged"/> event of the application's
        /// language manager.
        /// </summary>
        /// <param name="sender">The object which raised the event.</param>
        /// <param name="e">The event's arguments.</param>
        /// <remarks>
        /// <para>If the <see cref="ViewModels.LanguageManager.SelectedLanguage"/> property of the
        /// application's language manager has changed, the application's title should also be
        /// invalidated.</para>
        /// </remarks>
        private void LanguageManager_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == LanguageManager.SelectedLanguagePropertyName)
                OnPropertyChanged(TitlePropertyName);
        }

        /// <summary>
        /// Handles the <see cref="ViewModelBase.PropertyChanged"/> event of the current game of
        /// Untangle loaded in the application.
        /// </summary>
        /// <param name="sender">The object which raised the event.</param>
        /// <param name="e">The event's arguments.</param>
        /// <remarks>
        /// <para>If the <see cref="ViewModels.Game.LevelNumber"/> property of the current game of
        /// Untangle loaded in the application has changed, the application's title should also be
        /// invalidated.</para>
        /// </remarks>
        private void Game_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Game.LevelNumberPropertyName)
                OnPropertyChanged(TitlePropertyName);
        }
    }
}