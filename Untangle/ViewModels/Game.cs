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
using System.Windows;
using Untangle.Generation;
using Untangle.Resources;

namespace Untangle.ViewModels
{
    /// <summary>
    /// A view model class for a single game of Untangle.
    /// </summary>
    public class Game : ViewModelBase
    {
        /// <summary>
        /// Property name constant for the current game level's number.
        /// </summary>
        public const string LevelNumberPropertyName = "LevelNumber";
        /// <summary>
        /// Property name constant for the current game level.
        /// </summary>
        public const string LevelPropertyName = "Level";

        /// <summary>
        /// The current game level's number.
        /// </summary>
        private int _levelNumber;
        /// <summary>
        /// The current game level.
        /// </summary>
        private GameLevel _level;

        /// <summary>
        /// The current game level's number.
        /// </summary>
        public int LevelNumber
        {
            get { return _levelNumber; }
            set
            {
                if (_levelNumber == value)
                    return;

                _levelNumber = value;
                OnPropertyChanged(LevelNumberPropertyName);
            }
        }

        /// <summary>
        /// The current game level.
        /// </summary>
        public GameLevel Level
        {
            get { return _level; }
            set
            {
                if (_level == value)
                    return;

                if (_level != null)
                    _level.LevelSolved -= Level_LevelSolved;
                _level = value;
                if (_level != null)
                    _level.LevelSolved += Level_LevelSolved;
                OnPropertyChanged(LevelPropertyName);
            }
        }

        /// <summary>
        /// Initializes a new <see cref="Game"/> instance with the specified start level number
        /// and generates the start level.
        /// </summary>
        /// <param name="startLevelNumber">The number of the start level of the Untangle game.
        /// </param>
        public Game(int startLevelNumber)
        {
            LevelNumber = startLevelNumber;
            GenerateCurrentLevel();
        }

        /// <summary>
        /// Initializes a new <see cref="Game"/> instance with the specified <see cref="Level"/>
        /// and <see cref="LevelNumber"/>.
        /// </summary>
        /// <param name="level">The current game level.</param>
        /// <param name="levelNumber">The current game level's number.</param>
        public Game(GameLevel level, int levelNumber)
        {
            Level = level;
            LevelNumber = levelNumber;
        }

        /// <summary>
        /// Generates the current level of the Untangle game.
        /// </summary>
        /// <remarks>
        /// <para>The number of vertices in the generate level depends on the current game level's
        /// number.</para>
        /// </remarks>
        private void GenerateCurrentLevel()
        {
            var levelGenerator = new LevelGenerator(1 + _levelNumber, 2 + _levelNumber, 4);
            Level = levelGenerator.GenerateLevel();
        }

        /// <summary>
        /// Handles the <see cref="GameLevel.LevelSolved"/> event of the current game level.
        /// </summary>
        /// <param name="sender">The object which raised the event.</param>
        /// <param name="e">The event's arguments.</param>
        /// <remarks>
        /// <para>When a game level is solved, a message is shown to the user and the next level is
        /// generated.</para>
        /// </remarks>
        private void Level_LevelSolved(object sender, EventArgs e)
        {
            MessageBox.Show(
                string.Format(Messages.LevelSolved, _levelNumber),
                Application.Current.MainWindow.Title);

            LevelNumber++;
            GenerateCurrentLevel();
        }
    }
}