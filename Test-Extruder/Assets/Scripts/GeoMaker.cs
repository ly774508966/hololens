﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.SpatialMapping;
using HoloLensXboxController;

public class GeoMaker: MonoBehaviour
{
  [Tooltip("Material to render selected patches with")]
  public Material selectionMaterial = null;

  [Tooltip("Material to render extruded meshes with")]
  public Material extrudeMaterial = null;

  private ControllerInput m_xboxController = null;
  private GameObject m_gameObject = null;
  private MeshCollider m_meshCollider = null;
  private Mesh m_mesh = null;
  private MeshRenderer m_meshRenderer = null;
  private PlanarTileSelection m_selection = null;
  private MeshExtruder m_meshExtruder = null;
  private enum State
  {
    Select,
    ExtrudeSimple,
    ExtrudeCapped,
    Finished
  }
  private State m_state = State.Select;
  private float m_extrudeLength = 0;
  private Vector2[] m_topUV;
  private Vector2[] m_sideUV;

  private void Update()
  {
    if (!PlayspaceManager.Instance.IsScanningComplete())
      return;

    // Get current joypad axis values
#if UNITY_EDITOR
    float hor = Input.GetAxis("Horizontal");
    float ver = Input.GetAxis("Vertical");
    bool buttonA = Input.GetKeyDown(KeyCode.Joystick1Button0);
    bool buttonB = Input.GetKeyDown(KeyCode.Joystick1Button1);
#else
    m_xboxController.Update();
    float hor = m_xboxController.GetAxisLeftThumbstickX();
    float ver = m_xboxController.GetAxisLeftThumbstickY();
    bool buttonA = m_xboxController.GetButtonDown(ControllerButton.A);
    bool buttonB = m_xboxController.GetButtonDown(ControllerButton.B);
#endif

    float delta = (Mathf.Abs(ver) > 0.25f ? ver : 0) * Time.deltaTime;

    if (m_state == State.Select)
    {
      m_selection.Raycast(Camera.main.transform.position, Camera.main.transform.forward);
      Vector3[] vertices;
      int[] triangles;
      Vector2[] uv;
      m_selection.GenerateMeshData(out vertices, out triangles, out uv);
      if (vertices.Length > 0)
      {
        m_mesh.Clear();
        m_mesh.vertices = vertices;
        m_mesh.uv = uv;
        m_mesh.triangles = triangles;
        m_mesh.RecalculateBounds();
        //TODO: make a GetTransform function
        m_gameObject.transform.rotation = m_selection.rotation;
        m_gameObject.transform.position = m_selection.position;
        m_gameObject.transform.localScale = m_selection.scale;
        if (buttonA)
        {
          m_state = State.ExtrudeSimple;
          m_meshExtruder = new MeshExtruder(m_selection);
        }
        else if (buttonB)
        {
          m_state = State.ExtrudeCapped;
          m_meshExtruder = new MeshExtruder(m_selection);
        }
      }
    }
    else if (m_state == State.ExtrudeSimple)
    {
      m_extrudeLength += delta;
      Vector3[] vertices;
      int[] triangles;
      Vector2[] uv;
      m_meshExtruder.ExtrudeSimple(out vertices, out triangles, out uv, m_extrudeLength, m_topUV, m_sideUV, m_extrudeLength * 100);// SelectionTile.SIDE_CM);
      m_mesh.Clear();
      m_mesh.vertices = vertices;
      m_mesh.uv = uv;
      m_mesh.triangles = triangles;
      m_mesh.RecalculateBounds();
      m_mesh.RecalculateNormals();
      m_meshRenderer.material = extrudeMaterial;
      m_meshCollider.sharedMesh = null;
      m_meshCollider.sharedMesh = m_mesh;
      if (buttonA)
        m_state = State.Finished;
    }
    else if (m_state == State.ExtrudeCapped)
    {
      m_extrudeLength += delta;
      Vector3[] vertices;
      int[] triangles;
      Vector2[] uv;
      m_meshExtruder.ExtrudeCapped(out vertices, out triangles, out uv, m_extrudeLength, m_topUV, m_sideUV, m_sideUV);
      m_mesh.Clear();
      m_mesh.vertices = vertices;
      m_mesh.uv = uv;
      m_mesh.triangles = triangles;
      m_mesh.RecalculateBounds();
      m_mesh.RecalculateNormals();
      m_meshRenderer.material = extrudeMaterial;
      m_meshCollider.sharedMesh = null;
      m_meshCollider.sharedMesh = m_mesh;
      if (buttonA)
        m_state = State.Finished;
    }
  }

  private void Awake()
  {
    // Create reticle game object and mesh
    m_gameObject = new GameObject("Extruded");
    m_gameObject.transform.parent = null;
    m_meshCollider = m_gameObject.AddComponent<MeshCollider>();
    m_mesh = m_gameObject.AddComponent<MeshFilter>().mesh;
    m_meshRenderer = m_gameObject.AddComponent<MeshRenderer>();
    m_meshRenderer.material = selectionMaterial;
    m_meshRenderer.material.color = Color.white;
    m_meshRenderer.enabled = true;

    // Selection surface
    Vector2[] tileUV =
    {
      new Vector2((1f/128) * 0.5f, (1f/736) * (736 - 0.5f)),
      new Vector2((1f/128) * (128 - 0.5f), (1f/736) * (736 - 0.5f)),
      new Vector2((1f/128) * (128 - 0.5f), (1f/736) * 0.5f),
      new Vector2((1f/128) * 0.5f, (1f/736) * 0.5f)
      /*
      (1f / 128) * new Vector2(3.5f, 128 - 3.5f),
      (1f / 128) * new Vector2(128 - 3.5f, 128 - 3.5f),
      (1f / 128) * new Vector2(128 - 3.5f, 3.5f),
      (1f / 128) * new Vector2(3.5f, 3.5f)
      */
    };
    m_topUV = tileUV;
    m_sideUV = tileUV;
    m_selection = new PlanarTileSelection(70, m_topUV);
#if !UNITY_EDITOR
    m_xboxController = new ControllerInput(0, 0.19f);
#endif
  }
}
