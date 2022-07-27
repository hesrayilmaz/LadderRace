// Animancer // Copyright 2020 Kybernetik //

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value.

using UnityEngine;

namespace Animancer.Examples.Locomotion
{
    /// <summary>
    /// A simple trigger that teleports anything exiting it over to the left.
    /// </summary>
    [AddComponentMenu(Strings.MenuPrefix + "Examples/Locomotion - Screen Bounds Teleporter")]
    [HelpURL(Strings.APIDocumentationURL + ".Examples.Locomotion/ScreenBoundsTeleporter")]
    public sealed class ScreenBoundsTeleporter : MonoBehaviour
    {
        /************************************************************************************************************************/

        [SerializeField] private BoxCollider _Collider;

        /************************************************************************************************************************/

        private void Update()
        {
            var camera = Camera.main;
            if (camera == null)
                return;

            var position = camera.transform.position;
            position.z = 0;
            transform.position = position;

            var topLeft = camera.ScreenPointToRay(Vector3.zero).origin;
            _Collider.size = (position - topLeft) * 2;
        }

        /************************************************************************************************************************/

        private void OnTriggerExit(Collider collider)
        {
            var position = collider.transform.position;
            position.x -= (position.x - transform.position.x) * 2;
            collider.transform.position = position;
        }

        /************************************************************************************************************************/
    }
}
