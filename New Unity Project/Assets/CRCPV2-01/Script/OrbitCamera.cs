using UnityEngine;
using System.Collections;
     
    [AddComponentMenu("Camera-Control/Mouse drag Orbit with zoom")]
    public class OrbitCamera : MonoBehaviour
    {
    public Transform target;
	public bool autoRotateOn = false;
    public bool autoRotateReverse = false;
    public float autoRotateSpeed = 1f;
    float originalAutoRotateSpeed;
    public float autoRotateSpeedFast = 5f;
    float autoRotateValue = 1;
    public float distance = 1.5f;
#if UNITY_ANDROID
    public float xSpeed = 1.0f;
    public float ySpeed = 1.0f;
#else
    public float xSpeed = 15.0f;
    public float ySpeed = 15.0f;
#endif
    public float yMinLimit = -20f;
    public float yMaxLimit = 80f;
     
    public float distanceMin = 1f;
    public float distanceMax = 3f;
     
    public float smoothTime = 2f;
	public float autoTimer = 5f;
     
    float rotationYAxis = 0.0f;
    float rotationXAxis = 0.0f;
     
    float velocityX = 0.0f;
    float velocityY = 0.0f;
	bool faster;
	private bool rkeyActive;


     

void Start()
{
	rkeyActive = autoRotateOn;
	autoRotateValue = 1;
    Vector3 angles = transform.eulerAngles;
    rotationYAxis = angles.y;
    rotationXAxis = angles.x;
	originalAutoRotateSpeed = autoRotateSpeed;
    if (GetComponent<Rigidbody>())
    {
		GetComponent<Rigidbody>().freezeRotation = true;
    }
}
	
	
private void Update()
{

	if (autoRotateOn)
        {
			velocityX += (autoRotateSpeed * autoRotateValue) * Time.deltaTime;
        }
	if (Input.GetKeyUp ("r") && autoRotateOn == false){
	autoRotateOn = true;
	rkeyActive = true;
	
	}else if(Input.GetKeyUp ("r") && autoRotateOn == true){
	autoRotateOn = false;
	rkeyActive = false;
	}
	
	if (Input.GetKeyDown(KeyCode.LeftShift) && (!faster))
	{
		faster = true;
		autoRotateSpeed = autoRotateSpeedFast;
		autoRotateOn = true;
	}
	
	if (Input.GetKeyUp(KeyCode.LeftShift) && (faster))
	{
		faster = false;
		autoRotateSpeed = originalAutoRotateSpeed;
		if (rkeyActive == false){
			autoRotateOn = false;
		}
	}
	
	if (autoRotateReverse == true)
	{
		autoRotateValue = -1;
    }
	else
	{
		autoRotateValue = 1;
	}


	
}

	 
void LateUpdate()
{
    if (target != null)
    {
		if (Input.GetMouseButton(0))
		{
			velocityX += xSpeed * Input.GetAxis("Mouse X") * distance * 0.02f ;
			velocityY += ySpeed * Input.GetAxis("Mouse Y") * 0.02f;
		}
     
		rotationYAxis += velocityX;
		rotationXAxis -= velocityY;
     
		rotationXAxis = ClampAngle(rotationXAxis, yMinLimit, yMaxLimit);
     
		Quaternion toRotation = Quaternion.Euler(rotationXAxis, rotationYAxis, 0);
		Quaternion rotation = toRotation;
		distance = Mathf.Clamp(distance - Input.GetAxis("Mouse ScrollWheel") * 5, distanceMin, distanceMax);
     
		RaycastHit hit;
		if (Physics.Linecast(target.position, transform.position, out hit))
		{
			distance -= hit.distance;
		}
		Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);
		Vector3 position = rotation * negDistance + target.position;
		transform.rotation = rotation;
		transform.position = position;
     
		velocityX = Mathf.Lerp(velocityX, 0, Time.deltaTime * smoothTime);
		velocityY = Mathf.Lerp(velocityY, 0, Time.deltaTime * smoothTime);
		}
	    else
        {
            Debug.LogWarning("Orbit Camera - No Target Set");
        }
    }
     
    public static float ClampAngle(float angle, float min, float max)
    {
		if (angle < -360F)
		angle += 360F;
		if (angle > 360F)
		angle -= 360F;
		return Mathf.Clamp(angle, min, max);
    }
}
     
     

