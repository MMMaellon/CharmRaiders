
using MMMaellon.P_Shooters;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class PlayerGunModeSetter : UdonSharpBehaviour
{
    void Start()
    {
        SetPistol();
    }
    public AltFire alt;
    public SimpleReload reload;

    public int extraCapacity = 0;

    public int pistolCapacity = 8;

    [System.NonSerialized, UdonSynced, FieldChangeCallback(nameof(rifle))]
    public bool _rifle = false;//+rapid fire, 3x capacity
    [System.NonSerialized, UdonSynced, FieldChangeCallback(nameof(shotgun))]
    public bool _shotgun = false;//+spread, +damage, 0.5x capacity
    [System.NonSerialized, UdonSynced, FieldChangeCallback(nameof(sniper))]
    public bool _sniper = false;//+scope, ++damage, 0.25x capacity
    [System.NonSerialized, UdonSynced, FieldChangeCallback(nameof(katana))]
    public bool _katana = false;//+instakill, melee


    public bool katana
    {
        get => _katana;
        set
        {
            _katana = value;
            reload.magCapacity = 0;
            CalcAppearance();
            if (Networking.LocalPlayer.IsOwner(gameObject))
            {
                RequestSerialization();
            }
        }
    }
    public bool rifle
    {
        get => _rifle;
        set
        {
            _rifle = value;
            reload.magCapacity = CalcCapacity();
            CalcAppearance();
            if (Networking.LocalPlayer.IsOwner(gameObject))
            {
                RequestSerialization();
                reload.Reload();
            }
        }
    }
    public bool shotgun{
        get => _shotgun;
        set
        {
            _shotgun = value;
            reload.magCapacity = CalcCapacity();
            CalcAppearance();
            if (Networking.LocalPlayer.IsOwner(gameObject)){
                RequestSerialization();
                reload.Reload();
            }
        }
    }
    public bool sniper{
        get => _sniper;
        set {
            _sniper = value;
            reload.magCapacity = CalcCapacity();
            CalcAppearance();
            if (Networking.LocalPlayer.IsOwner(gameObject)){
                RequestSerialization();
                reload.Reload();
            }
        }
    }

    public void CalcAppearance()
    {
        if (_katana)
        {
            alt.altFire = 5;
            return;
        }
        if (rifle)
        {
            alt.rapidFire = true;
        }
        if (sniper && shotgun)
        {
            alt.altFire = 4;
        }
        else if (sniper)
        {
            alt.altFire = 3;
        }
        else if (shotgun)
        {
            alt.altFire = 2;
        }
        else if (rifle)
        {
            alt.altFire = 1;
        }
        else
        {
            alt.altFire = 0;
        }
    }

    public int CalcCapacity()
    {
        if (_katana)
        {
            return 0;
        }
        int capacity = pistolCapacity;
        if (rifle)
        {
            capacity *= 3;
        }
        if (shotgun)
        {
            capacity = Mathf.CeilToInt(capacity / 2f);
        }
        if (sniper)
        {
            capacity = Mathf.CeilToInt(capacity / 4f);
        }
        return capacity + extraCapacity;
    }

    public void SetPistol()
    {
        if (Networking.LocalPlayer.IsOwner(gameObject))
        {
            katana = false;
            rifle = false;
            shotgun = false;
            sniper = false;
        }
    }

    public void SetRifle()
    {
        if (Networking.LocalPlayer.IsOwner(gameObject))
        {
            rifle = true;
        }
    }

    public void SetShotgun()
    {
        if (Networking.LocalPlayer.IsOwner(gameObject))
        {
            shotgun = true;
        }
    }

    public void SetSniper()
    {
        if (Networking.LocalPlayer.IsOwner(gameObject))
        {
            sniper = true;
        }
    }
}
