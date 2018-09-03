using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextureCsController : MonoBehaviour
{
    [SerializeField] ComputeShader compute_shader_;
    private static readonly int kWidth = 512;
    private static readonly int kHeight = 512;
    private int kernel_index_;
    private RenderTexture render_texture_;
    private uint thread_size_x_, thread_size_y_, thread_size_z_;
    private RawImage output_image_;

    // Use this for initialization
    private void Start()
    {
        output_image_ = GetComponent<RawImage>();

        render_texture_ = new RenderTexture(kWidth, kHeight, 0, RenderTextureFormat.ARGB32);
        render_texture_.enableRandomWrite = true;
        render_texture_.Create();

        kernel_index_ = compute_shader_.FindKernel("CSMain");
        compute_shader_.SetTexture(kernel_index_, "texture_buffer", render_texture_);
        compute_shader_.GetKernelThreadGroupSizes(kernel_index_, out thread_size_x_, out thread_size_y_, out thread_size_z_);
    }

    // Update is called once per frame
    private void Update()
    {
        // グループ数は「テクスチャの水平(垂直)方向の解像度 / 水平(垂直)方向のスレッド数」で算出しています
        int group_size_x = kWidth / (int)thread_size_x_;
        int group_size_y = kHeight / (int)thread_size_y_;
        compute_shader_.Dispatch(kernel_index_, group_size_x, group_size_y, (int)thread_size_z_);
        output_image_.texture = render_texture_;
    }

    private void OnDestroy()
    {
        render_texture_.Release();
    }
}
