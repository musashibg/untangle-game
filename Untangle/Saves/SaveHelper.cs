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
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;
using Microsoft.Win32;
using Untangle.Resources;
using Untangle.ViewModels;

namespace Untangle.Saves
{
    /// <summary>
    /// A helper class that provides methods for saving and loading of games.
    /// </summary>
    public static class SaveHelper
    {
        /// <summary>
        /// The game's current version number.
        /// </summary>
        private const int CurrentVersion = 1;
        /// <summary>
        /// The default extension for saved game files.
        /// </summary>
        private const string SavedGameExtension = ".usg";
        /// <summary>
        /// The XML element name of the element containing the validation hash of a saved game.
        /// </summary>
        private const string HashElementName = "Hash";

        /// <summary>
        /// The directory of the saved game file which was most recently saved or loaded.
        /// </summary>
        private static string _lastSavedGamePath;

        /// <summary>
        /// Initializes <see cref="_lastSavedGamePath"/> with a default value of "&lt;User
        /// Documents Folder&gt;\My Games\Untangle" and creates that folder if it does not
        /// exist yet.
        /// </summary>
        static SaveHelper()
        {
            _lastSavedGamePath = string.Format(
                @"{0}\My Games\Untangle",
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

            // Create default saved game directory if needed
            if (!Directory.Exists(_lastSavedGamePath))
                Directory.CreateDirectory(_lastSavedGamePath);
        }

        /// <summary>
        /// Saves a game to a file chosen by the user.
        /// </summary>
        /// <param name="game">The game to be saved.</param>
        /// <returns><see langword="true"/> if the user has chosen to save the game and the saved
        /// game file has been created successfully.</returns>
        public static bool SaveGame(Game game)
        {
            string fileName;
            if (!PromptForFileToSave(out fileName))
                return false;

            // Create saved vertex objects
            var savedVertices = new Dictionary<ViewModels.Vertex, Vertex>();
            int idCounter = 0;
            foreach (ViewModels.Vertex vertex in game.Level.GameObjects.OfType<ViewModels.Vertex>())
            {
                var savedVertex = new Vertex
                {
                    Id = idCounter++,
                    X = vertex.X,
                    Y = vertex.Y,
                };
                savedVertices[vertex] = savedVertex;
            }

            // Attach connected vertex IDs to saved vertex objects
            foreach (KeyValuePair<ViewModels.Vertex, Vertex> pair in savedVertices)
            {
                pair.Value.ConnectedVertexIds = pair.Key.ConnectedVertices
                    .Select(d => savedVertices[d].Id)
                    .ToArray();
            }

            // Create saved game objects
            var savedGame = new SavedGame
            {
                Version = CurrentVersion,
                CreationDate = DateTime.Now,
                LevelNumber = game.LevelNumber,
                VertexCount = game.Level.VertexCount,
                IntersectionCount = game.Level.IntersectionCount,
                Vertices = savedVertices.Values.ToArray(),
            };

            // Save the saved game object to the specified file
            SaveToFile(savedGame, fileName);

            return true;
        }

        /// <summary>
        /// Loads a previously saved game from a file chosen by the user.
        /// </summary>
        /// <param name="game">The game which has been loaded from the chosen file.</param>
        /// <returns><see langword="true"/> if the user has chosen a valid saved game file to load
        /// and the game has been loaded into <paramref name="game"/> successfully.</returns>
        /// <exception cref="Exception">
        /// The chosen saved game file is damaged or does not contain a valid Untangle saved game.
        /// 
        /// -or-
        /// The chosen saved game file was created by a newer version of the game.
        /// </exception>
        public static bool LoadGame(out Game game)
        {
            string fileName;
            if (!PromptForFileToLoad(out fileName))
            {
                game = null;
                return false;
            }

            // Load the saved game object from the specified file
            SavedGame savedGame = LoadFromFile(fileName);

            // Verify saved game version
            if (savedGame.Version > CurrentVersion)
                throw new Exception(ExceptionMessages.SavedGameVersionNotSupported);

            // Verify that game level vertices are saved
            if (savedGame.Vertices == null)
                throw new Exception(ExceptionMessages.DamagedSavedGame);

            // Create the game and return it
            GameLevel gameLevel = GameLevel.Create(savedGame.Vertices);
            game = new Game(gameLevel, savedGame.LevelNumber);
            return true;
        }

        /// <summary>
        /// Prompts the user for a file which a game should be saved to.
        /// </summary>
        /// <param name="fileName">The full file name chosen by the user.</param>
        /// <returns><see langword="true"/> if the user has chosen a file which the game should be
        /// saved to.</returns>
        private static bool PromptForFileToSave(out string fileName)
        {
            var dialog = new SaveFileDialog
            {
                Title = DialogStrings.SaveGameDialogTitle,
                Filter = DialogStrings.SavedGameFilesFilter,
                DefaultExt = SavedGameExtension,
                InitialDirectory = _lastSavedGamePath,
                OverwritePrompt = true,
            };

            if (dialog.ShowDialog() == true)
            {
                fileName = dialog.FileName;
                _lastSavedGamePath = Path.GetDirectoryName(fileName);
                return true;
            }
            else
            {
                fileName = null;
                return false;
            }
        }

        /// <summary>
        /// Prompts the user for a saved game file which could be loaded.
        /// </summary>
        /// <param name="fileName">The full file name chosen by the user.</param>
        /// <returns><see langword="true"/> if the user has chosen a saved game file which should
        /// be loaded.</returns>
        private static bool PromptForFileToLoad(out string fileName)
        {
            var dialog = new OpenFileDialog
            {
                Title = DialogStrings.LoadGameDialogTitle,
                Filter = DialogStrings.SavedGameFilesFilter,
                DefaultExt = SavedGameExtension,
                InitialDirectory = _lastSavedGamePath,
                CheckFileExists = true,
            };

            if (dialog.ShowDialog() == true)
            {
                fileName = dialog.FileName;
                _lastSavedGamePath = Path.GetDirectoryName(fileName);
                return true;
            }
            else
            {
                fileName = null;
                return false;
            }
        }

        /// <summary>
        /// Wrties a <see cref="SavedGame"/> object to a specific file.
        /// </summary>
        /// <param name="savedGame">An object storing the information about the saved game.</param>
        /// <param name="fileName">The name of the file which the saved game should be written to.
        /// </param>
        private static void SaveToFile(SavedGame savedGame, string fileName)
        {
            // Get raw saved game XML document
            XDocument savedGameXml;
            using (var stream = new MemoryStream())
            {
                var xmlSerializer = new XmlSerializer(typeof(SavedGame));
                xmlSerializer.Serialize(stream, savedGame);
                stream.Position = 0;
                savedGameXml = XDocument.Load(stream);
            }

            // Compute hash on the raw saved game XML document and append it to the document
            string base64Hash = GetSavedGameHash(savedGameXml);
            var hashElement = new XElement(HashElementName, base64Hash);
            savedGameXml.Root.Add(hashElement);

            // Compress the saved game XML document and write it to the specified file
            using (FileStream fileStream = File.Create(fileName))
            using (var compressionStream = new DeflateStream(fileStream, CompressionMode.Compress))
            {
                savedGameXml.Save(compressionStream, SaveOptions.DisableFormatting);
            }
        }

        /// <summary>
        /// Reads a <see cref="SavedGame"/> object from a specific file.
        /// </summary>
        /// <param name="fileName">The name of the file which the saved game should be read from.
        /// </param>
        /// <returns>An object storing the information about the previously saved game.</returns>
        /// <exception cref="Exception">
        /// The chosen saved game file is damaged or does not contain a valid Untangle saved game.
        /// </exception>
        private static SavedGame LoadFromFile(string fileName)
        {
            // Read and decompress the saved game XML document from the specified file
            XDocument savedGameXml;
            try
            {
                using (FileStream fileStream = File.OpenRead(fileName))
                using (var decompressionStream = new DeflateStream(fileStream, CompressionMode.Decompress))
                {
                    savedGameXml = XDocument.Load(decompressionStream);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ExceptionMessages.DamagedSavedGame, ex);
            }

            // Find the hash element in the saved game XML document and remove it from it
            XElement hashElement = savedGameXml.Root.Element(HashElementName);
            if (hashElement == null)
                throw new Exception(ExceptionMessages.DamagedSavedGame);
            hashElement.Remove();

            // Calculate the hash of the raw saved game XML document and ensure it matches the
            // saved hash
            string base64Hash = GetSavedGameHash(savedGameXml);
            if (hashElement.Value != base64Hash)
                throw new Exception(ExceptionMessages.DamagedSavedGame);

            // Deserialize the saved game and return it
            try
            {
                using (var stream = new MemoryStream())
                {
                    savedGameXml.Save(stream, SaveOptions.DisableFormatting);
                    stream.Position = 0;
                    var xmlSerializer = new XmlSerializer(typeof(SavedGame));
                    return (SavedGame)xmlSerializer.Deserialize(stream);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ExceptionMessages.DamagedSavedGame, ex);
            }
        }

        /// <summary>
        /// Computes the validation hash of a saved game.
        /// </summary>
        /// <param name="savedGameXml">The raw saved game XML document.</param>
        /// <returns>The base-64 encoded SHA1 hash of the raw saved game XML document.</returns>
        private static string GetSavedGameHash(XDocument savedGameXml)
        {
            byte[] savedGameBytes = Encoding.Unicode.GetBytes(
                savedGameXml.ToString(SaveOptions.DisableFormatting));
            using (SHA1 sha = new SHA1Managed())
            {
                byte[] hash = sha.ComputeHash(savedGameBytes);
                return Convert.ToBase64String(hash);
            }
        }
    }
}