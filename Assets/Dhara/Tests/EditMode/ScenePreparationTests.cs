using Dhara.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dhara.Tests
{
    public sealed class ScenePreparationTests
    {
        [Test]
        public void PreparingScenePreservesExistingObjectsAndConfiguration()
        {
            var scene=EditorSceneManager.NewPreviewScene();
            try
            {
                var original=new GameObject("Preserved scene object"); SceneManager.MoveGameObjectToScene(original,scene);
                var first=DharaProject.EnsureBootstrap(scene,null,null); first.City.Seed=1234;
                var second=DharaProject.EnsureBootstrap(scene,null,null);
                Assert.That(second,Is.SameAs(first)); Assert.That(second.City.Seed,Is.EqualTo(1234));
                Assert.That(original!=null,Is.True); Assert.That(scene.rootCount,Is.EqualTo(2));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
