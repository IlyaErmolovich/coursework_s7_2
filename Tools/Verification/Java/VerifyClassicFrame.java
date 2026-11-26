import com.winterduel.bluetooth.ClassicFrame;
import java.io.*;
public class VerifyClassicFrame {
 public static void main(String[] args)throws Exception{
  ByteArrayOutputStream bytes=new ByteArrayOutputStream();DataOutputStream out=new DataOutputStream(bytes);
  String[] messages={"{\"type\":\"hello\"}","{\"text\":\"Снежок ❄\"}","x".repeat(32000)};
  for(String s:messages)ClassicFrame.write(out,s);
  ByteArrayInputStream fragmented=new ByteArrayInputStream(bytes.toByteArray()){
   public synchronized int read(byte[] b,int offset,int length){return super.read(b,offset,Math.min(length,3));}
  };
  DataInputStream in=new DataInputStream(fragmented);
  for(String expected:messages)if(!ClassicFrame.read(in).equals(expected))throw new AssertionError("Frame corrupted");
  for(int bad:new int[]{-1,0,65537}){
   bytes.reset();out.writeInt(bad);
   try{ClassicFrame.read(new DataInputStream(new ByteArrayInputStream(bytes.toByteArray())));throw new AssertionError("Bad length accepted");}catch(IOException expected){}
  }
  try{ClassicFrame.read(new DataInputStream(new ByteArrayInputStream(new byte[]{0,0,0,20,1})));throw new AssertionError("Truncated frame accepted");}catch(EOFException expected){}
  System.out.println("PASS: split/coalesced UTF-8 frames, large payload, length limits and EOF");
 }
}
