using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class BackgroundController : MonoBehaviour
{
    private float m_StartPosX, m_LengthOfBG;
    public GameObject m_Camera;
    public float m_ParallaxEffectMagnitude; // The speed at which the background should move relative to the camera.


    // Start is called before the first frame update
    void Start()
    {
        m_StartPosX = this.transform.position.x;

        m_LengthOfBG = GetComponent<SpriteRenderer>().bounds.size.x;
    }

    void FixedUpdate()
    {
        // Calculate the distance background move based on cam movement 
        float distance = m_Camera.transform.position.x * m_ParallaxEffectMagnitude; // 0 = move with cam || 1 = won't move || 0.5 = half.
        this.transform.position = new Vector3(m_StartPosX+distance,  transform.position.y, transform.position.z);



        float movement = m_Camera.transform.position.x * (1 - m_ParallaxEffectMagnitude);
        if (movement > (m_StartPosX + m_LengthOfBG))
        {
            m_StartPosX += m_LengthOfBG;
        }
        else if (movement < (m_StartPosX - m_LengthOfBG))
        {
            m_StartPosX -= m_LengthOfBG;
        }

    }
}
