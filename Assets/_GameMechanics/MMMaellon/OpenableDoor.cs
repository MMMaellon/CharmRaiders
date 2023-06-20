
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Data;

namespace MMMaellon{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class OpenableDoor : CharmSpawn
    {
        public Transform objectParent;
        public Animator animator;
        public int maxHealth = 10;
        [UdonSynced(UdonSyncMode.None), System.NonSerialized, FieldChangeCallback(nameof(health))]
        public int _health = 10;
        public bool AllowClick = true;

        [System.NonSerialized]
        public DataList _listeners = new DataList();
        [System.NonSerialized]
        public OpenableDoorListener[] listeners = new OpenableDoorListener[0];
        [System.NonSerialized]
        public DataToken[] _listenerTokenArray = new DataToken[0];
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
                if (Networking.LocalPlayer.IsOwner(gameObject))
                {
                    if (value > 0 && health <= 0)
                    {
                        foreach (OpenableDoorListener listener in listeners)
                        {
                            if (listener != null)
                            {
                                listener.OnClose(this, Networking.LocalPlayer);
                            }
                        }
                    }
                    else if (value <= 0 && health > 0)
                    {
                        foreach (OpenableDoorListener listener in listeners)
                        {
                            if (listener != null)
                            {
                                listener.OnOpen(this, Networking.LocalPlayer);
                            }
                        }
                    }
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
            DisableInteractive = !AllowClick;
        }

        public override void Interact()
        {
            ToggleOpen();
        }

        public override void ResetSpawn()
        {
            ResetHealth();
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
            P_Shooters.P_Shooter shooter = other.GetComponentInParent<P_Shooters.P_Shooter>();
            if (!Utilities.IsValid(shooter) || !shooter.sync.IsLocalOwner())
            {
                return;
            }

            damage = shooter.damage;
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
        [System.NonSerialized]
        public ChildAttachmentState lastCharm;
        public override void SpawnCharm(ChildAttachmentState charm)
        {
            lastCharm = charm;
            charm.transform.position = objectParent.position;
            lowestPoint = objectParent.position;
            charmCollider = charm.GetComponent<Collider>();
            if (Utilities.IsValid(charmCollider) && charmCollider.enabled)
            {
                lowestPoint.y = charmCollider.ClosestPoint(objectParent.position + (Vector3.down * 10000)).y;
            }
            foreach (Collider col in charm.GetComponentsInChildren<Collider>())
            {
                if (!col.enabled)
                {
                    continue;
                }
                nextLowestPoint = col.ClosestPoint(objectParent.position + (Vector3.down * 10000));
                if (lowestPoint.y > nextLowestPoint.y)
                {
                    lowestPoint.y = nextLowestPoint.y;
                }
            }
            charm.Attach(objectParent);
            charm.transform.rotation = objectParent.rotation;
            charm.transform.position = objectParent.position + (Vector3.up * Vector3.Distance(objectParent.position, lowestPoint));
            charm.sync.pos = (Vector3.up * Vector3.Distance(objectParent.position, lowestPoint));
            charm.sync.rot = Quaternion.identity;
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
            if (Utilities.IsValid(charmCollider) && charmCollider.enabled)
            {
                lowestPoint.y = charmCollider.ClosestPoint(objectParent.position + (Vector3.down * 10000)).y;
            }
            foreach (Collider col in lastCharm.GetComponentsInChildren<Collider>())
            {
                if (!col.enabled)
                {
                    continue;
                }
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

        public void AddListener(OpenableDoorListener listener)
        {
            if (!_listeners.Contains(listener))
            {
                _listeners.Add(listener);
                listeners = new OpenableDoorListener[_listeners.Count];
                _listenerTokenArray = _listeners.ToArray();
                for (int i = 0; i < _listeners.Count; i++)
                {
                    listeners[i] = (OpenableDoorListener) _listenerTokenArray[i].Reference;
                }
            }
        }

        public void RemoveListener(OpenableDoorListener listener)
        {
            if (_listeners.Contains(listener))
            {
                _listeners.RemoveAll(listener);
                listeners = new OpenableDoorListener[_listeners.Count];
                _listenerTokenArray = _listeners.ToArray();
                for (int i = 0; i < _listeners.Count; i++)
                {
                    listeners[i] = (OpenableDoorListener)_listenerTokenArray[i].Reference;
                }
            }
        }
    }
}