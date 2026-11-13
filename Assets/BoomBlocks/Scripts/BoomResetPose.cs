using UnityEngine;

namespace BoomBlocks {

    /// <summary>
    /// Remembers the starting pose of a physics prop (pillar, slab..) so the round can be restarted
    /// </summary>
    public class BoomResetPose : MonoBehaviour {

        Vector3 startPosition;
        Quaternion startRotation;
        Rigidbody rigid;

        void Awake() {
            startPosition = transform.position;
            startRotation = transform.rotation;
            rigid = GetComponent<Rigidbody>();
        }

        public void ResetPose() {
            if (rigid != null) {
                rigid.linearVelocity = Vector3.zero;
                rigid.angularVelocity = Vector3.zero;
                rigid.position = startPosition;
                rigid.rotation = startRotation;
            }
            transform.SetPositionAndRotation(startPosition, startRotation);
        }
    }
}
