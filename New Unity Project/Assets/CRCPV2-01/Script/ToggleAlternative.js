public var cars : GameObject[];
public var carsPolice : GameObject[];
public var keyPolice : KeyCode;
// What car is active at start
private var carNumber:int = 0;

function Start ()
{
	for( var i: int=0; i < cars.Length; i++){
	cars[i].SetActive(false);
	}
	cars[carNumber].SetActive(true);
}

    function Update(){
		// Toggle Police Version of the car
		if (cars[carNumber].activeInHierarchy == true && Input.GetKeyDown (keyPolice)){
		cars[carNumber].SetActive(false);
		carsPolice[carNumber].SetActive(true);
		}
		else if(Input.GetKeyDown (keyPolice))
		{
		cars[carNumber].SetActive(true);
		carsPolice[carNumber].SetActive(false);
		}
		//
		

		
    }
     


 
