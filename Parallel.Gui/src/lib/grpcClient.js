import grpc from '@grpc/grpc-js';
import protoLoader from '@grpc/proto-loader';
import path from 'path';
import { fileURLToPath } from 'url'

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const PROTO_PATH = path.join(__dirname, 'proto/greeter.proto');
const packageDefinition = protoLoader.loadSync(PROTO_PATH, {
  keepCase: true,
  longs: String,
  enums: String,
  defaults: true,
  oneofs: true
});

const proto = grpc.loadPackageDefinition(packageDefinition).greet;

// Connect to gRPC server
const client = new proto.Greeter(
    '127.0.0.1:5000',
    grpc.credentials.createInsecure()
  );
  
export default client;