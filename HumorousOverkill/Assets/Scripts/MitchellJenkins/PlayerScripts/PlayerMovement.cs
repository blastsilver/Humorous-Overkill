﻿using UnityEngine;
using EventHandler;

public class PlayerMovement : MonoBehaviour {

    private CharacterController m_cc;
    private PlayerCamera m_camera;
    private PlayerInfo m_ply;
    private Transform m_transform;
    private Animator m_animator;

    public Vector3 m_moveDirection = Vector3.zero;
    private float m_horizontal = 0f;
    private float m_vertical = 0f;
    public float m_moveSpeed = 10f;
    private float m_airV = 0f;
    private float m_gravity = 20f;
    private float m_jumpHeight = 10f;

    public LayerMask m_groundMask;
    public bool m_grounded = true;

    void Start () {
        
        m_camera    = this.GetComponentInChildren<PlayerCamera>();
        m_cc        = this.GetComponent<CharacterController>() as CharacterController;
        //m_ply       = this.GetComponent<Player>()._PlayerInfo;
        //m_animator  = this.GetComponent<Player>()._Animator;
        m_transform = this.transform;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update () {
        transform.Rotate(0f, Input.GetAxis("Mouse X") * 200 * m_camera.m_sensitivity * Time.deltaTime, 0f);

        m_moveDirection.x = Input.GetAxis("Horizontal") * m_moveSpeed;
        m_moveDirection.z = Input.GetAxis("Vertical") * m_moveSpeed;
        m_moveDirection = this.transform.TransformDirection(m_moveDirection);

        if (Input.GetKeyDown(KeyCode.Space) && m_grounded) {
            Jump();
        }
        
        if (Input.GetKey(KeyCode.LeftControl)) {
            if (m_cc.height > 1.1) { m_cc.height = Mathf.Lerp(m_cc.height, 1f, Time.deltaTime * 10f); } else { m_cc.height = 1f; }
            m_moveSpeed = 5f;
        } else {
            if (m_cc.height < 1.9) { m_cc.height = Mathf.Lerp(m_cc.height, 2f, Time.deltaTime * 10f); } else { m_cc.height = 2f; }
            if (Input.GetKey(KeyCode.LeftShift)) {
                m_moveSpeed = 15f;
            } else {
                m_moveSpeed = 10f;
            }
        }
        Debug.DrawLine(this.transform.position + Vector3.down, this.transform.position + Vector3.down * 1.3f, Color.cyan);
        if (Physics.Raycast(this.transform.position + Vector3.down, Vector3.down, 0.3f, m_groundMask )) {
            m_grounded = true;
        } else {
            m_grounded = false;
        }

        m_moveDirection.y -= m_gravity * Time.deltaTime;
        m_cc.Move(m_moveDirection * Time.deltaTime);
    }

    private void Jump () {
        m_moveDirection.y = m_jumpHeight;
        m_airV -= Time.deltaTime;
    }
}