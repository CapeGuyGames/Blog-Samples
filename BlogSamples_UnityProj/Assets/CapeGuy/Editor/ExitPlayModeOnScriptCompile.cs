// Copyright Cape Guy Ltd. 2015. http://capeguy.co.uk.
// Provided under the terms of the MIT license -
// http://opensource.org/licenses/MIT. Cape Guy accepts
// no responsibility for any damages, financial or otherwise,
// incurred as a result of using this code.

using UnityEngine;
using UnityEditor;

/// <summary>
/// This script exits play mode whenever script compilation is detected during an editor update.
/// See https://capeguy.co.uk/2015/06/no-more-unity-hot-reload/ for more information.
/// </summary>
[InitializeOnLoad] // Make static initialiser be called as soon as the scripts are initialised in the editor (rather than just in play mode).
public class ExitPlayModeOnScriptCompile {

	// Static initialiser called by Unity Editor whenever scripts are loaded (editor or play mode)
	static ExitPlayModeOnScriptCompile () {
		EditorApplication.update += OnEditorUpdate;
	}

	// Called each time the editor updates.
	private static void OnEditorUpdate () {
		if (EditorApplication.isPlaying && EditorApplication.isCompiling) {
			Debug.Log ("Exiting play mode due to script compilation.");
			EditorApplication.isPlaying = false;
		}
	}

}