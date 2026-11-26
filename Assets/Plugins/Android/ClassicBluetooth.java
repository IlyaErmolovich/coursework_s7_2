package com.winterduel.bluetooth;
import android.app.Activity;
import android.bluetooth.*;
import android.content.*;
import android.os.Build;
import android.os.Handler;
import android.os.Looper;
import org.json.JSONObject;
import java.io.*;
import java.util.UUID;
import java.util.concurrent.*;
import java.util.concurrent.atomic.AtomicInteger;

/** One secure Classic RFCOMM socket. No BLE, network or Unity calls from worker threads. */
public final class ClassicBluetooth {
 private static final UUID SERVICE=UUID.fromString("e0a981a7-3bf0-49fd-98c0-d4d812f75c70");
 private final Activity activity;
 private final BluetoothAdapter adapter;
 private final Handler ui=new Handler(Looper.getMainLooper());
 private final ArrayBlockingQueue<String> events=new ArrayBlockingQueue<>(1024);
 private final AtomicInteger generation=new AtomicInteger();
 private volatile BluetoothSocket socket;
 private volatile BluetoothServerSocket server;
 private volatile boolean connected,disposed;
 private volatile BlockingQueue<String> outgoing;
 private boolean registered;
 private final BroadcastReceiver receiver=new BroadcastReceiver(){
  @Override public void onReceive(Context context,Intent intent){
   if(disposed)return;
   try {
    String action=intent.getAction();
    if(BluetoothDevice.ACTION_FOUND.equals(action)){
     BluetoothDevice d=intent.getParcelableExtra(BluetoothDevice.EXTRA_DEVICE);if(d!=null)device(d);
    }else if(BluetoothAdapter.ACTION_DISCOVERY_FINISHED.equals(action))emit("scanDone","Поиск завершён. Можно повторить поиск.");
    else if(BluetoothAdapter.ACTION_STATE_CHANGED.equals(action)&&adapter!=null&&!adapter.isEnabled())fail(generation.get(),"Bluetooth выключен. Включите его и повторите подключение.");
   }catch(Exception e){emit("error","Не удалось получить устройство. Проверьте разрешения Bluetooth.");}
  }
 };
 public ClassicBluetooth(Activity activity){
  this.activity=activity;BluetoothManager manager=(BluetoothManager)activity.getSystemService(Context.BLUETOOTH_SERVICE);
  adapter=manager==null?null:manager.getAdapter();
  IntentFilter f=new IntentFilter();f.addAction(BluetoothDevice.ACTION_FOUND);f.addAction(BluetoothAdapter.ACTION_DISCOVERY_FINISHED);f.addAction(BluetoothAdapter.ACTION_STATE_CHANGED);
  if(Build.VERSION.SDK_INT>=33)activity.registerReceiver(receiver,f,Context.RECEIVER_EXPORTED);else activity.registerReceiver(receiver,f);
  registered=true;
 }
 public String poll(){String s=events.poll();return s==null?"":s;}
 private void emit(String type,String text){
  if(disposed)return;
  try{JSONObject o=new JSONObject();o.put("type",type);o.put("text",text);if(!events.offer(o.toString()))close();}catch(Exception ignored){}
 }
 private synchronized void emitCurrent(int token,String type,String text){if(token==generation.get())emit(type,text);}
 private void device(BluetoothDevice d)throws Exception{
  JSONObject o=new JSONObject();o.put("type","device");o.put("address",d.getAddress());String name=d.getName();o.put("text",name==null?"Устройство Bluetooth":name);o.put("paired",d.getBondState()==BluetoothDevice.BOND_BONDED);events.offer(o.toString());
 }
 public String state(){
  if(adapter==null)return "unsupported";
  try{return adapter.isEnabled()?"ready":"disabled";}catch(SecurityException e){return "permission";}
 }
 public void enable(){final int token=generation.get();ui.post(()->{if(token!=generation.get()||disposed)return;try{activity.startActivity(new Intent(BluetoothAdapter.ACTION_REQUEST_ENABLE));}catch(Exception e){emit("error","Не удалось открыть включение Bluetooth. Включите Bluetooth в настройках.");}});}
 public void discoverable(){final int token=generation.get();ui.post(()->{if(token!=generation.get()||disposed)return;try{Intent i=new Intent(BluetoothAdapter.ACTION_REQUEST_DISCOVERABLE);i.putExtra(BluetoothAdapter.EXTRA_DISCOVERABLE_DURATION,300);activity.startActivity(i);}catch(Exception e){emit("error","Не удалось включить видимость. Проверьте разрешение ближайших устройств.");}});}
 public void paired(){
  try{if(adapter!=null)for(BluetoothDevice d:adapter.getBondedDevices())device(d);}
  catch(SecurityException e){emit("error","Нет разрешения на доступ к сопряжённым устройствам.");}
  catch(Exception e){emit("error","Не удалось прочитать сопряжённые устройства.");}
 }
 public void scan(){
  try{if(adapter==null||!adapter.isEnabled()){emit("error","Включите Bluetooth и повторите поиск.");return;}
   adapter.cancelDiscovery();for(BluetoothDevice d:adapter.getBondedDevices())device(d);
   if(!adapter.startDiscovery())emit("error","Поиск не начался. На Android 11 и ниже проверьте разрешение и включение геолокации.");
   else emit("status","Поиск устройств… Выберите телефон, на котором создана игра.");
  }catch(SecurityException e){emit("error","Нет разрешения на поиск ближайших устройств.");}catch(Exception e){emit("error","Не удалось начать поиск. Повторите попытку.");}
 }
 public void host(){
  close();final int token=generation.get();
  new Thread(()->{
   BluetoothServerSocket local=null;
   try{
    local=adapter.listenUsingRfcommWithServiceRecord("Winter Duel",SERVICE);
    synchronized(this){if(token!=generation.get()){local.close();return;}server=local;}
    emitCurrent(token,"status","Ожидание второго игрока. Разрешите видимость телефона на 5 минут.");
    BluetoothSocket accepted=local.accept(120000);local.close();
    attach(accepted,token);
   }catch(Exception e){fail(token,"Ожидание завершено. Повторите создание игры.");}
  },"Winter-BT-accept").start();discoverable();
 }
 public void connect(String address){
  close();final int token=generation.get();
  new Thread(()->{
   try{
    adapter.cancelDiscovery();BluetoothDevice d=adapter.getRemoteDevice(address);
    BluetoothSocket local=d.createRfcommSocketToServiceRecord(SERVICE);
    synchronized(this){if(token!=generation.get()){local.close();return;}socket=local;}
    ui.postDelayed(()->{if(token==generation.get()&&!connected)fail(token,"Время подключения истекло. Подтвердите сопряжение на обоих телефонах и повторите.");},30000);
    emitCurrent(token,"status","Подключение… Подтвердите системный запрос сопряжения на обоих телефонах.");
    local.connect();attach(local,token);
   }catch(Exception e){fail(token,"Не удалось подключиться. Создайте игру на втором телефоне и повторите; проверьте сопряжение.");}
  },"Winter-BT-connect").start();
 }
 private void attach(BluetoothSocket local,int token)throws IOException{
  final BlockingQueue<String> queue=new ArrayBlockingQueue<>(256);
  synchronized(this){if(token!=generation.get()){local.close();return;}socket=local;connected=true;outgoing=queue;}
  emitCurrent(token,"connected","Соединение установлено");
  new Thread(()->{
   try{DataOutputStream out=new DataOutputStream(local.getOutputStream());while(token==generation.get())ClassicFrame.write(out,queue.take());}
   catch(Exception e){fail(token,"Соединение потеряно. Вернитесь в меню и подключитесь снова.");}
  },"Winter-BT-write").start();
  new Thread(()->{
   try{DataInputStream in=new DataInputStream(local.getInputStream());while(token==generation.get()){
    String message=ClassicFrame.read(in);emitCurrent(token,"message",message);
   }}catch(Exception e){fail(token,"Соединение потеряно. Вернитесь в меню и подключитесь снова.");}
  },"Winter-BT-read").start();
 }
 public void send(String message){BlockingQueue<String> q=outgoing;if(!connected||q==null)return;if(!q.offer(message))fail(generation.get(),"Соединение не успевает передавать данные. Подключитесь снова.");}
 private synchronized void fail(int token,String message){if(token!=generation.get()||disposed)return;close();emit("disconnected",message);}
 public synchronized void close(){
  generation.incrementAndGet();connected=false;
  // Wake the writer without retaining an idle thread per connection attempt.
  BlockingQueue<String> q=outgoing;outgoing=null;if(q!=null)q.offer("");
  try{if(server!=null)server.close();}catch(Exception ignored){}server=null;
  try{if(socket!=null)socket.close();}catch(Exception ignored){}socket=null;
  try{if(adapter!=null)adapter.cancelDiscovery();}catch(Exception ignored){}
  events.clear();
 }
 public void dispose(){close();disposed=true;if(registered){try{activity.unregisterReceiver(receiver);}catch(Exception ignored){}registered=false;}ui.removeCallbacksAndMessages(null);}
}
