## ***This guide is currently incomplete.***

# About this guide

This guide walks you through the elements of deploying a Lambda to a VPC with the required roles:

1. Terraform setup (required only for the terraform path)
1. Building a VPC
1. Configuring IAM
1. Building a Lambda

It shows how to do with in 3 paths:

1. Deploying via the AWS Console
1. Deploying with the AWS CLI
1. Deploying via terraform (with setup of S3 and Dynamo for state storage)

The associated Lambda source is found in the `/src` folder and provides the simplest Lambda function. You can install the AWS templates with this command:
```
dotnet tool install -g Amazon.Lambda.Tools
```
If you have done that, the associated Lambda can also be generated with this dotnet CLI command:
```
dotnet new serverless.AspNetCoreMinimalAPI -n Calculator
```
Note, for the following terraform code the associated folder structure is required.

The terraform commands are executed from the `/terraform/modules/1-main-module/` folder.

# Terraform Setup

**Important:** If you are using the AWS Console or AWS CLI you can skip this section. However, if you are using terraform, you will need to choose whether you are creating the terraform state resources via console or CLI, as these are required to use terraform.

## Terraform State Management

We will be using S3 and DynamoDB for terraform state management, so we need to create those resources manually...

## Create S3 bucket for terraform state store

## **Console**

1. Visit S3 console
1. 

## **CLI**

The bucket name must be globally unique - which means the name cannot exist anywhere else in S3 on any account. You need to change the number placeholder below to something random.

Create a unique S3 bucket for storing the terraform state:

```
aws s3api create-bucket --bucket my-bucket-[random-number] --region us-east-1 --acl private
```

Set the public access permissions to private:

```
aws s3api put-public-access-block --bucket my-bucket-[random-number] --public-access-block-configuration "BlockPublicAcls=true,IgnorePublicAcls=true,BlockPublicPolicy=true,RestrictPublicBuckets=true"
```

## Create DynamoDB table for terraform state lock

### **Console**

1. Visit DynamoDB console
1. 

### **CLI**

Create a DynamoDB table named `calculator-lock`:

```
aws dynamodb create-table --table-name calculator-lock --attribute-definitions AttributeName=LockID,AttributeType=S --key-schema AttributeName=LockID,KeyType=HASH --billing-mode PROVISIONED --provisioned-throughput ReadCapacityUnits=5,WriteCapacityUnits=5
```

## Initialise Terraform

### **CLI Only**

Intialise terraform in your local folder and point it at the S3 bucket and DynamoDB table create above:

```
terraform init \
-backend-config="bucket=my-personal-calculator-state-bucket" \
-backend-config="key=modules/calculator/terraform.tfstate" \
-backend-config="region=us-east-1" \
-backend-config="dynamodb_table=calculator-lock"
```

# Build VPC

Here we will build a VPC which contains a single private subnet. The subnet will have a single route table with a single rule which allows it access to only subnets within the VPC.

## **Console**



## **CLI**

## **Terraform**

# Configure IAM

A role requires 3 things:

* Trust policy (defines WHAT can assume the role)
* Managed or inline Permissions policy (defines what can be done when assuming the role)
* Role (links the Trust policy with the Permissions policy)

As many or few manage and/or inline permissions policies can be attached as required.

The sequence of events to create a role with a trust policy (known as a Trust Relationship) and its attached Permissions Policy is:

1. Create a trust policy file
1. Create the role, attaching the trust policy with it
1. Attach the permissions policy

## **Console**

*TBC*

## **CLI**

When defining the role you must also define the trust policy. However, when defining the permissions this is done with one of two options:

1. Using a managed policy from AWS (or your own, pre-created policy)
1. Defining and attaching your own, inline policy

We will cover both below.

## Define the trust policy

Let's create a trust policy file in JSON format. Save the following as `trust-policy.json`:
```
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Effect": "Allow",
            "Principal": {
                "Service": "lambda.amazonaws.com"
            },
            "Action": "sts:AssumeRole"
        }
    ]
}
```
This allows the Lambda service to assume the role.

## Create the role (and attach the trust policy)

The trust policy must be attached to the role when the role is created:

```
aws iam create-role --role-name TestRole --assume-role-policy file://trust-policy.json
```

*Note:* The `file://` is mandatory, or you will receive this error:
```
An error occurred (MalformedPolicyDocument) when calling the CreateRole operation: This policy contains invalid Json
```

## (Option 1) Attach a managed permission policy to the role

Once you have the ARN of the managed permission policy, you can attach it to your role. Here, we will use the `AWSLambdaBasicExecutionRole` which provides basic execution permissions for a Lambda:

```
aws iam attach-role-policy --policy-arn arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole --role-name TestRole
```

The policy attached here grants the lambda basic execution permissions.

As another example, the following managed permission policy grants the lambda permission to execute within a VPC:

```
aws iam attach-role-policy --policy-arn arn:aws:iam::aws:policy/service-role/AWSLambdaVPCAccessExecutionRole --role-name TestRole
```

## (Option 2) Attach an inline permission policy to the role

To attach an inline permission policy:

1. Create the policy JSON file
1. Attach the policy to the role

## (Option 2) Create the inline policy file

Save the following JSON as `basic-permission-inline-policy.json`. It simply copies the same permissions as the `AWSLambdaBasicExecutionRole` managed permission policy above:

```
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Effect": "Allow",
            "Action": [
                "logs:CreateLogGroup",
                "logs:CreateLogStream",
                "logs:PutLogEvents"
            ],
            "Resource": "*"
        }
    ]
}
```

## (Option 2) Attach the permission policy to the role

As above, be sure to include the `file://` otherwise you will receive an error:

```
aws iam put-role-policy --role-name TestRole --policy-name basic-lambda-execution-perms --policy-document file://basic-permission-inline-policy.json
```

## **Terraform**

## Create the role with a trust policy attached

```
resource "aws_iam_role" "role_with_trust_policy" {
  name = "lambda_trust_policy"
  description = ""

  assume_role_policy = <<EOF
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Principal": {
        "Service": "lambda.amazonaws.com"
      },
      "Action": "sts:AssumeRole"
    }
  ]
}
EOF
}
```

## (Option 1) Attach managed policy to role

```
resource "aws_iam_role_policy_attachment" "attach_managed_policy" {
  role       = aws_iam_role.role_with_trust_policy.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole"
}
```

## (Option 2) Attach inline policy to lambda role

```
resource "aws_iam_policy" "policy" {
  name        = "basic-lambda-execution-policy"
  description = "A copy of AWSLambdaBasicExecutionRole policy"

  policy = <<EOF
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "logs:CreateLogGroup",
        "logs:CreateLogStream",
        "logs:PutLogEvents"
      ],
      "Resource": "*"
    }
  ]
}
EOF
}

resource "aws_iam_role_policy_attachment" "attach_inline_policy" {
  role       = aws_iam_role.role_with_trust_policy.name
  policy_arn = aws_iam_policy.policy.arn
}
```

# Build Lambda

In this section we will construct a Lambda Function definition, hosted within the private subnet created earlier, and with the Function URL turned on. It will also use the role we created earlier.

You can create the Lambda yourself (see instructions at the top of the guide) or you can use the Lambda source provided in the `/src` folder. If you generated the Lambda yourself, be sure to place it in the same location.

## **Console**

*TBC*

## **CLI**

*TBC*

## **Terraform**



# References

* AWS CLI setup https://medium.com/@vishal.sharma./create-an-aws-s3-bucket-using-aws-cli-5a19bc1fda79
* https://awscli.amazonaws.com/v2/documentation/api/latest/reference/dynamodb/create-table.html
* https://docs.aws.amazon.com/cli/latest/reference/s3api/create-bucket.html
* https://awscli.amazonaws.com/v2/documentation/api/latest/reference/s3api/put-public-access-block.html
