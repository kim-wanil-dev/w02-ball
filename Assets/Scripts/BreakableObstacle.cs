using System.Collections.Generic;
using UnityEngine;

public class BreakbleObstacle : MonoBehaviour
{
    [Header("Collision")]
    [SerializeField] private float _requiredImpulseMagnitude = 1500f;

    [Header("Piece")]
    [SerializeField] private Rigidbody _piecePrefab;
    [SerializeField] private float _cubeSize = 5f;
    [SerializeField] private float _pieceLifetime = 3f;
    [SerializeField] private float _colliderDisableDelay = 0.3f;

    [Header("Explosion")]
    [SerializeField] private float _explosionForce = 5000f;
    [SerializeField] private float _explosionRadius = 35f;
    [SerializeField] private float _explosionUpward = 20f;


    private readonly List<Collider> _pieceColliders = new();
    private Collider _obstacleCollider;
    private Renderer[] _renderers;
    private bool _isExploded;

    private void Awake()
    {
        if (_obstacleCollider == null)
        {
            _obstacleCollider = GetComponent<Collider>();
        }

        _renderers = GetComponentsInChildren<Renderer>();
    }

    private void OnCollisionEnter(Collision other)
    {
        if (_isExploded)
            return;

        if (!other.gameObject.CompareTag("Player"))
            return;

        float impulseMagnitude = other.impulse.magnitude;

        Debug.Log($"충격량 크기: {impulseMagnitude}");

        if (impulseMagnitude >= _requiredImpulseMagnitude)
        {
            Explode();
        }
    }

    private void Explode()
    {
        _isExploded = true;

        HideOriginalObject();
        CreatePieces();

        Invoke(nameof(DisablePieceColliders), _colliderDisableDelay);

        Destroy(gameObject, _pieceLifetime);
    }

    private void CreatePieces()
    {
        if (_obstacleCollider is BoxCollider boxCollider)
        {
            CreateBoxPieces(boxCollider);
        }
        else if (_obstacleCollider is SphereCollider sphereCollider)
        {
            CreateSpherePieces(sphereCollider);
        }
        else
        {
            Debug.LogWarning(
                $"{name}: BreakableObstacle Component은 BoxCollider와 SphereCollider만 지원합니다."
            );
        }
    }

    private void CreateBoxPieces(BoxCollider boxCollider)
    {
        Vector3 scale = transform.lossyScale;

        Vector3 worldSize = new Vector3(
            boxCollider.size.x * Mathf.Abs(scale.x),
            boxCollider.size.y * Mathf.Abs(scale.y),
            boxCollider.size.z * Mathf.Abs(scale.z)
        );

        int xCount = Mathf.Max(1, Mathf.CeilToInt(worldSize.x / _cubeSize));
        int yCount = Mathf.Max(1, Mathf.CeilToInt(worldSize.y / _cubeSize));
        int zCount = Mathf.Max(1, Mathf.CeilToInt(worldSize.z / _cubeSize));

        Vector3 localPieceSize = new Vector3(
            _cubeSize / Mathf.Abs(scale.x),
            _cubeSize / Mathf.Abs(scale.y),
            _cubeSize / Mathf.Abs(scale.z)
        );

        Vector3 startPosition =
            boxCollider.center
            - boxCollider.size * 0.5f
            + localPieceSize * 0.5f;

        for (int x = 0; x < xCount; x++)
        {
            for (int y = 0; y < yCount; y++)
            {
                for (int z = 0; z < zCount; z++)
                {
                    Vector3 localPosition = startPosition + new Vector3(
                        x * localPieceSize.x,
                        y * localPieceSize.y,
                        z * localPieceSize.z
                    );

                    Vector3 worldPosition =
                        transform.TransformPoint(localPosition);

                    CreatePiece(worldPosition);
                }
            }
        }
    }

    private void CreateSpherePieces(SphereCollider sphereCollider)
    {
        Vector3 scale = transform.lossyScale;

        float maxScale = Mathf.Max(
            Mathf.Abs(scale.x),
            Mathf.Abs(scale.y),
            Mathf.Abs(scale.z)
        );

        float worldRadius = sphereCollider.radius * maxScale;

        Vector3 worldCenter =
            transform.TransformPoint(sphereCollider.center);

        int count = Mathf.CeilToInt(
            worldRadius * 2f / _cubeSize
        );

        Vector3 startPosition =
            worldCenter
            - Vector3.one * worldRadius
            + Vector3.one * (_cubeSize * 0.5f);

        float allowedRadius =
            Mathf.Max(0f, worldRadius - _cubeSize * 0.5f);

        float allowedRadiusSqr =
            allowedRadius * allowedRadius;

        for (int x = 0; x < count; x++)
        {
            for (int y = 0; y < count; y++)
            {
                for (int z = 0; z < count; z++)
                {
                    Vector3 position = startPosition + new Vector3(
                        x * _cubeSize,
                        y * _cubeSize,
                        z * _cubeSize
                    );

                    Vector3 offset =
                        position - worldCenter;

                    if (offset.sqrMagnitude > allowedRadiusSqr)
                        continue;

                    CreatePiece(position);
                }
            }
        }
    }

    private void CreatePiece(Vector3 position)
    {
        Rigidbody pieceRigidbody = Instantiate(
            _piecePrefab,
            position,
            transform.rotation
        );

        pieceRigidbody.transform.localScale =
            Vector3.one * _cubeSize;

        Collider pieceCollider =
            pieceRigidbody.GetComponent<Collider>();

        if (pieceCollider != null)
        {
            _pieceColliders.Add(pieceCollider);
        }

        pieceRigidbody.AddExplosionForce(
            _explosionForce,
            transform.position,
            _explosionRadius,
            _explosionUpward
        );

        Destroy(
            pieceRigidbody.gameObject,
            _pieceLifetime
        );
    }

    private void DisablePieceColliders()
    {
        foreach (Collider pieceCollider in _pieceColliders)
        {
            if (pieceCollider != null)
            {
                pieceCollider.enabled = false;
            }
        }

        _pieceColliders.Clear();
    }

    private void HideOriginalObject()
    {
        foreach (Renderer objectRenderer in _renderers)
        {
            objectRenderer.enabled = false;
        }

        if (_obstacleCollider != null)
        {
            _obstacleCollider.enabled = false;
        }
    }
}