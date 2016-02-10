/**
 * oscP5parsing by andreas schlegel
 * example shows how to parse incoming osc messages "by hand".
 * it is recommended to take a look at oscP5plug for an
 * alternative and more convenient way to parse messages.
 * oscP5 website at http://www.sojamo.de/oscP5
 */

import oscP5.*;
import netP5.*;

OscP5 oscP5;
NetAddress myRemoteLocation;

void setup() {
//  size(400,400);
//  frameRate(60);
  /* start oscP5, listening for incoming messages at port 12000 */
  oscP5 = new OscP5(this,7000);
  
  myRemoteLocation = new NetAddress("10.142.212.71",12001);
//  noWindow();
}

void draw() {
//  background(0);  
}


void mousePressed() {
  
}


void oscEvent(OscMessage theOscMessage) {
  /* check if theOscMessage has the address pattern we are looking for. */
  OscMessage msg = new OscMessage("/test");
  msg.add("a msg!");
  oscP5.send(theOscMessage,myRemoteLocation);
  
  theOscMessage.print();
  
  if(theOscMessage.checkAddrPattern("/rigidBody")) {
    
    
    
  } 
//  println("### received an osc message. with address pattern "+theOscMessage.addrPattern());

}
