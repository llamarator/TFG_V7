/*
 * demo sketch for PLX DAQ v2 
 * Moving Real Time Data Diagramm
 */

bool analog_ref = 0;
unsigned long n_muestra=0;
int Ts =50;
int n_documento =0;
int readvalue = 0;
short int logic_analyzer = 0;

String str;
char firstChar ;

double val=0;
double voltage_derivative=0;
double interval = 0;
double val_prev=0;
double ref = 0;

unsigned long n_sample;
unsigned long previousTime=0;
unsigned long time_t=0;
unsigned long retardo = 0;
unsigned long retardo_prev =0;

void setup() {
    analog_ref = 1;
    analogReference(DEFAULT);
    Serial.begin(38400);
    if(Ts==0)Ts=10;
    //analogReference(INTERNAL);
    pinMode(2, INPUT);
    pinMode(3, INPUT);
    attachInterrupt(digitalPinToInterrupt(2),ISR_falling, FALLING);
    //attachInterrupt(digitalPinToInterrupt(3),ISR_falling, FALLING);

}
void ISR_rising()
{
  logic_analyzer = 1;
  n_documento++;
  }
  void ISR_falling()
{
  n_documento++;
  logic_analyzer = 0;
  }

void loop() {
  retardo = millis();
  
       if(val/10000.0 < 0.9 && analog_ref == 1)
       {
        analog_ref = 0;
        analogReference(INTERNAL);
        for(int z=0;z<20;z++)//para acondicionar el ADC a la nueva referencia
        {
        val = analogRead(A0)* (10000.0 / 1023.0);
        val = (val + analogRead(A1)* (10000.0 / 1023.0))/2;
        }
       }
       else if(val/10000.0 > 0.9 && analog_ref == 0){
        analog_ref = 1;
       analogReference(DEFAULT);
       for(int z=0;z<20;z++)//para acondicionar el ADC a la nueva referencia
        {
        val = analogRead(A0)* (50000.0 / 1023.0);
        val = (val + analogRead(A1)* (50000.0 / 1023.0))/2;
        }
       }
       
       firstChar='0';
      if(Serial.available() > 0)
      {
        str = Serial.readStringUntil('\n');
        firstChar = str.charAt(0);
        switch(firstChar){
        case '1':
           n_muestra = 0 ; 
           readvalue =0;
           n_documento=0;
           val=0;
           val_prev=0;
        break;
        case '2':
          n_documento++;
        break;
        case '5':
          n_muestra=0;
          val=1.33;
          val_prev=1.33;
        break;
        }
      }
      
       previousTime = time_t;
       val_prev=val;
       val=0;
       if(analog_ref == 0){
        val = analogRead(A0)* (10000.0 / 1023.0);
        val = (val + analogRead(A1)* (10000.0 / 1023.0))/2;
       }else {
        val = analogRead(A0)* (50000.0 / 1023.0);
        val = (val + analogRead(A1)* (50000.0 / 1023.0))/2;
       }
        time_t = millis();
        interval = (time_t - previousTime);
        voltage_derivative= (val-val_prev)/interval; // V/us  
        n_muestra++; 
        

  Serial.println( (String) n_muestra + ";" + (val-ref) + ";" + Ts + ";" + firstChar + ";" + n_documento); //depuracion

        retardo=  millis()-retardo;
        retardo_prev=retardo;
        
  delay(Ts-retardo);      
  
}
