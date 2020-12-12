using System.Xml.Linq;
using ModApi.Ui;
using UI.Xml;

namespace Assets.Scripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using ModApi;
    using ModApi.Common;
    using ModApi.Mods;
    using UnityEngine;
    using ModApi.Scenes.Events;

    /// <summary>
    /// A singleton object representing this mod that is instantiated and initialize when the mod is loaded.
    /// </summary>
    public class Mod : ModApi.Mods.GameMod
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="Mod"/> class from being created.
        /// </summary>
        private Mod() : base()
        {
        }

        /// <summary>
        /// Gets the singleton instance of the mod object.
        /// </summary>
        /// <value>The singleton instance of the mod object.</value>
        public static Mod Instance { get; } = GetModInstance<Mod>();

        //private ImportButton _importButton;
        public string objDirectory;

        protected override void OnModInitialized()
        {
            objDirectory = Application.persistentDataPath + "/UserData/Mesh2Craft/Models/";
            System.IO.Directory.CreateDirectory(objDirectory);

            base.OnModInitialized();
            //Game.Instance.SceneManager.SceneLoaded += OnSceneLoaded;

            //Game.Instance.SceneManager.SceneTransitionStarted += (s, e) => _importButton = null;

            ImportButton.Initialize();
        }

        

        //public void OnSceneLoaded(object s, SceneEventArgs e)
        //{
        //    if (e.Scene == ModApi.Scenes.SceneNames.Designer)
        //    {
        //        var ui = Game.Instance.UserInterface;
        //        _importButton = ui.BuildUserInterfaceFromResource<ImportButton>("Mesh2Craft/Designer/MtoCButton"
        //            , (script, controller) => script.OnLayoutRebuilt(controller));

        //    }
        //}
    }
}