﻿using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(Rigidbody))]
public class Player : GameEventListener {

    private Animator m_animator;
    private PlayerInfo m_ply;
    private CharacterController m_cc;
    private RuntimeAnimatorController m_animatorController;
    private Rigidbody m_rb;

    [SerializeField] private bool m_cameraEnabled = true;
    [SerializeField] private bool m_movementEnabled = true;

    void Awake () {
        m_ply = GameObject.FindGameObjectWithTag("Manager").GetComponent<PlayerManager>().GetPlayerInfo;
        m_cc = this.GetComponent<CharacterController>() as CharacterController;
        m_rb = this.GetComponent<Rigidbody>() as Rigidbody;
    }

    public CharacterController _CharacterController {
        get { return this.m_cc; }
    }
    public Animator _Animator {
        get { return this.m_animator; }
    }
    public PlayerInfo _PlayerInfo {
        get { return m_ply; }
    }

    public bool isHealthFull{
        get { return m_ply.m_playerHealth == 100 ? true : false; }
    }
    public bool isDead {
        get { return m_ply.m_playerHealth == 0 ? true : false; }
    }
    public void AddHealth (int health) {
        m_ply.m_playerHealth += health; CheckHealth();
    }
    public void TakeDamage(int damage) {
        m_ply.m_playerHealth -= damage; CheckHealth();
    }
    
    void CheckHealth () {
        if (m_ply.m_playerHealth > 100) m_ply.m_playerHealth = 100;
        else if (m_ply.m_playerHealth < 0) m_ply.m_playerHealth = 0;
    }

    void Start () {
        m_animator.runtimeAnimatorController = m_animatorController;
        m_cc.center = new Vector3(0f, 1f, 0f);
        m_cc.height = 1.8f;
    }

    public override void HandleEvent (GameEvent e, float amount) {
        
    }
}