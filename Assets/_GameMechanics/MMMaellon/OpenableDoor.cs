
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class OpenableDoor : CharmSpawn
    {
        public Transform objectParent;
        public Animator animator;
        public int maxHealth = 10;
        [UdonSynced(UdonSyncMode.None), System.NonSerialized, FieldChangeCallback(nameof(health))]
        public int _health = 10;
        public int health
        {
            get => _health;
            set
            {
                animator.enabled = true;
                if (value < _health)
                {
                    animator.SetTrigger("bounce");
                }
                _health = value;
                if (maxHealth > 0)
                {
                    animator.SetFloat("health", (float)_health / (float)maxHealth);
                } else
                {
                    animator.SetFloat("health", _health > 0 ? 1.0f : -1001f);
                }
                if (Networking.LocalPlayer.IsOwner(gameObject))
                {
                    RequestSerialization();
                }
            }
        }
        void Start()
        {
            ResetHealth();
        }

        public override void Interact()
        {
            ToggleOpen();
        }

        public void ResetHealth()
        {
            health = maxHealth;
        }

        public void ToggleOpen()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            if (health <= 0)
            {
                ResetHealth();
            } else
            {
                health = -1001;
            }
        }

        int damage;
        public void OnParticleCollision(GameObject other)
        {
            P_Shooter shooter = other.GetComponentInParent<P_Shooter>();
            if (!Utilities.IsValid(shooter) || !shooter.sync.IsLocalOwner())
            {
                return;
            }

            damage = shooter.calcDamage();
            if (damage <= 0)
            {
                return;
            }

            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            if (damage > health)
            {
                health = 0;
            } else
            {
                health -= damage;
            }
        }

        public void DisableAnimator()
        {
            animator.enabled = false;
        }

        public void TestDamage()
        {
            health -= 1;
        }

        Collider charmCollider;
        Vector3 lowestPoint;
        Vector3 nextLowestPoint;
        ChildAttachmentState lastCharm;
        public override void SpawnCharm(ChildAttachmentState charm)
        {
            lastCharm = charm;
            charm.transform.position = objectParent.position;
            lowestPoint = objectParent.position;
            charmCollider = charm.GetComponent<Collider>();
            if (Utilities.IsValid(charmCollider))
            {
                lowestPoint.y = charmCollider.ClosestPoint(objectParent.position + (Vector3.down * 10000)).y;
            }
            foreach (Collider col in charm.GetComponentsInChildren<Collider>())
            {
                nextLowestPoint = col.ClosestPoint(objectParent.position + (Vector3.down * 10000));
                if (lowestPoint.y > nextLowestPoint.y)
                {
                    lowestPoint.y = nextLowestPoint.y;
                }
            }
            charm.transform.rotation = objectParent.rotation;
            charm.transform.position = objectParent.position + (Vector3.up * Vector3.Distance(objectParent.position, lowestPoint));
            charm.Attach(objectParent);
            SendCustomEventDelayedFrames(nameof(ReCheckLowestPoint), 1);
        }

        public void ReCheckLowestPoint()
        {
            if (!Utilities.IsValid(lastCharm))
            {
                return;
            }
            lowestPoint = objectParent.position;
            charmCollider = lastCharm.GetComponent<Collider>();
            if (Utilities.IsValid(charmCollider))
            {
                lowestPoint.y = charmCollider.ClosestPoint(objectParent.position + (Vector3.down * 10000)).y;
            }
            foreach (Collider col in lastCharm.GetComponentsInChildren<Collider>())
            {
                nextLowestPoint = col.ClosestPoint(objectParent.position + (Vector3.down * 10000));
                if (lowestPoint.y > nextLowestPoint.y)
                {
                    lowestPoint.y = nextLowestPoint.y;
                }
            }
            lastCharm.transform.rotation = objectParent.rotation;
            lastCharm.transform.position = objectParent.position + (Vector3.up * Vector3.Distance(objectParent.position, lowestPoint));
            lastCharm.sync.Serialize();
        }
    }
}