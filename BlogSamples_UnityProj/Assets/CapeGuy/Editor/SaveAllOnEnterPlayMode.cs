// Copyright Cape Guy Ltd. 2015. http://capeguy.co.uk.
// Provided under the terms of the MIT license -
// http://opensource.org/licenses/MIT. Cape Guy accepts
// no responsibility for any damages, financial or otherwise,
// incurred as a result of using this code.

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This script saves the current project and scene (if there is one) whenever the Unity editor enters play mode.
/// For more information see https://capeguy.co.uk/2015/07/unity-auto-save-on-play-in-editor/.
/// </summary>
[InitializeOnLoad]
public class SaveAllOnEnterPlayMode {

	static SaveAllOnEnterPlayMode ()
	{

		EditorApplication.playmodeStateChanged = () =>
		{

			if (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)
			{
				Debug.Log ("Auto-Saving scene(s) and assets before entering play mode: " + string.Join(", ", openSceneNames.ToArray()));

				EditorSceneManager.SaveOpenScenes();
				AssetDatabase.SaveAssets();
			}

		};

	}

	static IList<string> openSceneNames
	{
		get {
			int numScenes = SceneManager.sceneCount;
			IList<string> openScenes = new List<string> (numScenes);
			for (int currSceneIndex = 0; currSceneIndex < numScenes; ++currSceneIndex) {
				openScenes.Add(SceneManager.GetSceneAt(currSceneIndex).name);
			}
			return openScenes;
		}
	}

}