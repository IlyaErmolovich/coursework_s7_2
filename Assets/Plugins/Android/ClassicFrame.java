package com.winterduel.bluetooth;
import java.io.*;
import java.nio.charset.StandardCharsets;
/** Length-prefixed UTF-8 framing shared by both RFCOMM endpoints. */
public final class ClassicFrame {
 public static final int MAX = 65536;
 public static String read(DataInputStream in) throws IOException {
  int length=in.readInt();
  if(length<=0||length>MAX)throw new IOException("Invalid frame length");
  byte[] data=new byte[length];in.readFully(data);return new String(data,StandardCharsets.UTF_8);
 }
 public static void write(DataOutputStream out,String message) throws IOException {
  byte[] data=message.getBytes(StandardCharsets.UTF_8);
  if(data.length==0||data.length>MAX)throw new IOException("Invalid frame length");
  out.writeInt(data.length);out.write(data);out.flush();
 }
}
