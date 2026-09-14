using UnityEngine;
using UnityEngine.Splines;

public class SphereFriend : MonoBehaviour
{
    //스플라인을 2개 넣어두고 1번 지나서 대기
    // 플레이어가 올라오면 2번 출발 하면서 시네머신 이동. 
    [SerializeField] private float startOffset;
    [SerializeField] private float stopPoint;
    [SerializeField] private SplineContainer _splineRoute;
    private SplineAnimate _splineAnimate;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GetComponent<MeshRenderer>().material.color = Random.ColorHSV();
        _splineAnimate = GetComponent<SplineAnimate>();
        float targetStartTime = _splineAnimate.Duration * startOffset;
        _splineAnimate.ElapsedTime = targetStartTime;
        _splineAnimate.Play();
    }
    void Update()
    {
        if (_splineAnimate.NormalizedTime - startOffset > stopPoint && _splineAnimate.Duration > 20)
        {
            _splineAnimate.Pause();
        }
    }

    public void SetRoute()
    {
        _splineAnimate.ElapsedTime /= _splineAnimate.Duration / 15;
        _splineAnimate.Duration = 15f;
        _splineAnimate.Play();

        // Invoke("ChangeSplineRoute", 1 - (startOffset * 5));
    }

    private void ChangeSplineRoute()
    {
        _splineAnimate.ElapsedTime = 0;
        _splineAnimate.Container = _splineRoute;
        _splineAnimate.Play();

    }
}
